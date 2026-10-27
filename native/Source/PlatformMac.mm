#include "Platform.h"
#import <Cocoa/Cocoa.h>
#import <Carbon/Carbon.h>
#import <ApplicationServices/ApplicationServices.h>
#include <map>
#include <unistd.h>

// A real non-activating NSPanel is important: an ordinary floating NSWindow
// can still activate this application when clicked.
@interface HebrewMarksPanel : NSPanel
@end
@implementation HebrewMarksPanel
- (BOOL)canBecomeKeyWindow { return NO; }
- (BOOL)canBecomeMainWindow { return NO; }
@end

namespace
{
juce::String stringProperty(TISInputSourceRef source, CFStringRef key)
{
    auto value=static_cast<CFStringRef>(TISGetInputSourceProperty(source,key));
    if (!value) return {};
    const auto size=CFStringGetMaximumSizeForEncoding(CFStringGetLength(value),kCFStringEncodingUTF8)+1;
    std::vector<char> text(static_cast<size_t>(size));
    return CFStringGetCString(value,text.data(),size,kCFStringEncodingUTF8) ? juce::String::fromUTF8(text.data()) : juce::String();
}
bool sourceLanguage(TISInputSourceRef source, bool hebrew)
{
    auto languages=static_cast<CFArrayRef>(TISGetInputSourceProperty(source,kTISPropertyInputSourceLanguages));
    if (!languages) return false;
    for (CFIndex i=0; i<CFArrayGetCount(languages); ++i)
    {
        auto language=static_cast<CFStringRef>(CFArrayGetValueAtIndex(languages,i));
        if (CFEqual(language,hebrew ? CFSTR("he") : CFSTR("en"))) return true;
    }
    return false;
}
bool selectable(TISInputSourceRef source)
{
    auto category=static_cast<CFStringRef>(TISGetInputSourceProperty(source,kTISPropertyInputSourceCategory));
    auto enabled=static_cast<CFBooleanRef>(TISGetInputSourceProperty(source,kTISPropertyInputSourceIsEnabled));
    auto capable=static_cast<CFBooleanRef>(TISGetInputSourceProperty(source,kTISPropertyInputSourceIsSelectCapable));
    return category && CFEqual(category,kTISCategoryKeyboardInputSource)
        && enabled && CFBooleanGetValue(enabled) && capable && CFBooleanGetValue(capable);
}
pid_t editorPid()
{
    auto app=[[NSWorkspace sharedWorkspace] frontmostApplication];
    const auto pid=app ? [app processIdentifier] : 0;
    return pid==getpid() ? 0 : pid;
}

class MacPlatform final : public Platform
{
public:
    ~MacPlatform() override
    {
        if (hotkey) UnregisterEventHotKey(hotkey);
        if (handler) RemoveEventHandler(handler);
        if (component) component->removeFromDesktop();
        [panel orderOut:nil];
        [panel release];
    }
    void createWindow(juce::Component& c, std::function<void()> toggle) override
    {
        component=&c;
        onToggle=std::move(toggle);
        [NSApp setActivationPolicy:NSApplicationActivationPolicyAccessory];
        panel=[[HebrewMarksPanel alloc] initWithContentRect:NSMakeRect(0,0,c.getWidth(),c.getHeight())
            styleMask:NSWindowStyleMaskBorderless|NSWindowStyleMaskNonactivatingPanel
            backing:NSBackingStoreBuffered defer:NO];
        [panel setTitle:@"Hebrew Marks"];
        [panel setReleasedWhenClosed:NO];
        [panel setBecomesKeyOnlyIfNeeded:YES];
        [panel setHidesOnDeactivate:NO];
        [panel setLevel:NSFloatingWindowLevel];
        [panel setHasShadow:YES];
        [panel setCollectionBehavior:NSWindowCollectionBehaviorCanJoinAllSpaces|NSWindowCollectionBehaviorFullScreenAuxiliary];
        c.addToDesktop(juce::ComponentPeer::windowIgnoresKeyPresses, [panel contentView]);
        c.setTopLeftPosition(0,0);
        EventTypeSpec spec {kEventClassKeyboard,kEventHotKeyPressed};
        if (InstallApplicationEventHandler(hotkeyCallback,1,&spec,this,&handler)==noErr)
        {
            EventHotKeyID id {0x484d6172,1};
            RegisterEventHotKey(kVK_ANSI_H,controlKey|optionKey,id,GetApplicationEventTarget(),0,&hotkey);
        }
    }
    void setBounds(juce::Rectangle<int> r) override
    {
        if (!panel) return;
        position=r;
        const CGFloat screenTop=NSMaxY([[[NSScreen screens] objectAtIndex:0] frame]);
        [panel setFrame:NSMakeRect(r.getX(),screenTop-r.getBottom(),r.getWidth(),r.getHeight()) display:YES];
        component->setBounds(0,0,r.getWidth(),r.getHeight());
    }
    juce::Rectangle<int> bounds() const override { return position; }
    void show(bool visible) override
    {
        component->setVisible(visible);
        if (visible) [panel orderFrontRegardless]; else [panel orderOut:nil];
    }
    bool hotkeyAvailable() const override { return hotkey!=nullptr; }
    KeyboardState keyboard() override
    {
        KeyboardState s;
        s.hasTarget=editorPid()>0;
        s.needsPermission=!CGPreflightPostEventAccess();
        auto current=TISCopyCurrentKeyboardInputSource();
        if (current)
        {
            if (s.hasTarget)
            {
                s.hebrew=sourceLanguage(current,true);
                s.english=sourceLanguage(current,false);
                s.name=s.hebrew ? "Hebrew" : s.english ? "English" : stringProperty(current,kTISPropertyLocalizedName);
                if (s.hebrew || s.english) remembered[s.hebrew]=stringProperty(current,kTISPropertyInputSourceID);
            }
            if (pending)
            {
                if (editorPid()!=pendingPid) { selectionError="Click your document first"; pending=false; }
                else if (stringProperty(current,kTISPropertyInputSourceID)==requested) pending=false;
                else if (juce::Time::getMillisecondCounterHiRes()>deadline)
                { selectionError="Switch keyboard in editor"; pending=false; }
            }
            CFRelease(current);
        }
        else if (pending && juce::Time::getMillisecondCounterHiRes()>deadline)
        { selectionError="Switch keyboard in editor"; pending=false; }
        auto list=TISCreateInputSourceList(nullptr,false);
        if (list)
        {
            for (CFIndex i=0; i<CFArrayGetCount(list); ++i)
            {
                auto source=static_cast<TISInputSourceRef>(const_cast<void*>(CFArrayGetValueAtIndex(list,i)));
                if (selectable(source))
                { s.hebrewAvailable |= sourceLanguage(source,true); s.englishAvailable |= sourceLanguage(source,false); }
            }
            CFRelease(list);
        }
        return s;
    }
    juce::String insert(char16_t character) override
    {
        if (pending) return "Wait for keyboard change";
        const auto pid=editorPid();
        if (!pid) return "Click your document first";
        if (!CGPreflightPostEventAccess()) return "Allow Accessibility access";
        const auto flags=CGEventSourceFlagsState(kCGEventSourceStateCombinedSessionState);
        if (flags & (kCGEventFlagMaskShift|kCGEventFlagMaskControl|kCGEventFlagMaskAlternate|kCGEventFlagMaskCommand))
            return "Release modifier keys";
        auto current=TISCopyCurrentKeyboardInputSource();
        const bool hebrew=current && sourceLanguage(current,true);
        if (current) CFRelease(current);
        if (!hebrew) return "Select Hebrew keyboard";
        auto source=CGEventSourceCreate(kCGEventSourceStatePrivate);
        auto down=CGEventCreateKeyboardEvent(source,0,true);
        auto up=CGEventCreateKeyboardEvent(source,0,false);
        if (source) CFRelease(source);
        if (!down || !up)
        { if (down) CFRelease(down); if (up) CFRelease(up); return "Could not create input"; }
        const UniChar text=character;
        for (auto event : {down,up})
        { CGEventSetFlags(event,0); CGEventKeyboardSetUnicodeString(event,1,&text); }
        current=TISCopyCurrentKeyboardInputSource();
        const bool stillHebrew=current && sourceLanguage(current,true);
        if (current) CFRelease(current);
        juce::String result;
        if (editorPid()!=pid) result="Click your document first";
        else if (!stillHebrew) result="Select Hebrew keyboard";
        else
        {
            // Post to the captured application, so an application switch cannot
            // redirect this pair into a different program. No clipboard use.
            CGEventPostToPid(pid,down);
            CGEventPostToPid(pid,up);
        }
        CFRelease(down);
        CFRelease(up);
        return result;
    }
    juce::String selectKeyboard(bool hebrew) override
    {
        if (pending) return "Wait for keyboard change";
        const auto pid=editorPid();
        if (!pid) return "Click your document first";
        auto current=TISCopyCurrentKeyboardInputSource();
        const bool already=current && sourceLanguage(current,hebrew);
        if (current) CFRelease(current);
        if (already) return {};
        auto list=TISCreateInputSourceList(nullptr,false);
        if (!list) return "Keyboard not installed";
        TISInputSourceRef selected=nullptr;
        for (CFIndex i=0; i<CFArrayGetCount(list); ++i)
        {
            auto candidate=static_cast<TISInputSourceRef>(const_cast<void*>(CFArrayGetValueAtIndex(list,i)));
            if (selectable(candidate) && sourceLanguage(candidate,hebrew))
            {
                if (!selected) selected=candidate;
                if (remembered.count(hebrew) && stringProperty(candidate,kTISPropertyInputSourceID)==remembered[hebrew])
                { selected=candidate; break; }
            }
        }
        juce::String result;
        if (!selected) result="Keyboard not installed";
        else if (editorPid()!=pid) result="Click your document first";
        else if (TISSelectInputSource(selected)!=noErr) result="Switch keyboard in editor";
        else
        {
            requested=stringProperty(selected,kTISPropertyInputSourceID);
            pendingPid=pid;
            pending=true;
            deadline=juce::Time::getMillisecondCounterHiRes()+1000;
        }
        CFRelease(list);
        return result;
    }
    bool selectionPending() const override { return pending; }
    juce::String takeSelectionError() override { auto e=selectionError; selectionError.clear(); return e; }
    void requestPermission() override
    {
        CGRequestPostEventAccess();
        [[NSWorkspace sharedWorkspace] openURL:[NSURL URLWithString:@"x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility"]];
    }
private:
    static OSStatus hotkeyCallback(EventHandlerCallRef,EventRef event,void* data)
    {
        EventHotKeyID id{};
        if (GetEventParameter(event,kEventParamDirectObject,typeEventHotKeyID,nullptr,sizeof(id),nullptr,&id)==noErr
            && id.signature==0x484d6172 && id.id==1)
        { static_cast<MacPlatform*>(data)->onToggle(); return noErr; }
        return eventNotHandledErr;
    }
    HebrewMarksPanel* panel=nil;
    juce::Component* component=nullptr;
    juce::Rectangle<int> position;
    EventHotKeyRef hotkey=nullptr;
    EventHandlerRef handler=nullptr;
    std::function<void()> onToggle;
    std::map<bool,juce::String> remembered;
    bool pending=false;
    pid_t pendingPid=0;
    double deadline=0;
    juce::String requested, selectionError;
};
}
std::unique_ptr<Platform> makePlatform() { return std::make_unique<MacPlatform>(); }
