#include "Platform.h"
#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>
#include <commctrl.h>
#include <map>
#include <optional>

namespace
{
constexpr int hotkeyId=0x484d;
int language(HKL layout) { return static_cast<int>(reinterpret_cast<uintptr_t>(layout)&0x3ff); }
struct Target { HWND window{}, focus{}; DWORD thread{}; HKL layout{}; };
std::optional<Target> target()
{
    Target t;
    t.window=GetForegroundWindow();
    DWORD process=0;
    const auto thread=GetWindowThreadProcessId(t.window,&process);
    if (!thread || process==GetCurrentProcessId()) return {};
    GUITHREADINFO info{};
    info.cbSize=sizeof(info);
    if (!GetGUIThreadInfo(thread,&info) || !info.hwndFocus) return {};
    t.focus=info.hwndFocus;
    t.thread=GetWindowThreadProcessId(t.focus,nullptr);
    t.layout=GetKeyboardLayout(t.thread);
    if (!t.thread || GetForegroundWindow()!=t.window) return {};
    return t;
}
bool same(const Target& t, const std::optional<Target>& other)
{ return other && t.window==other->window && t.focus==other->focus && t.thread==other->thread; }
std::vector<HKL> installed()
{
    const int n=GetKeyboardLayoutList(0,nullptr);
    if (n<=0) return {};
    std::vector<HKL> layouts(static_cast<size_t>(n));
    const int copied=GetKeyboardLayoutList(n,layouts.data());
    layouts.resize(static_cast<size_t>(juce::jmax(0,copied)));
    return layouts;
}

class WindowsPlatform final : public Platform
{
public:
    ~WindowsPlatform() override
    {
        if (window) { UnregisterHotKey(window,hotkeyId); RemoveWindowSubclass(window,windowProc,1); }
    }
    void createWindow(juce::Component& c, std::function<void()> toggle) override
    {
        component=&c;
        onToggle=std::move(toggle);
        c.addToDesktop(juce::ComponentPeer::windowIsTemporary | juce::ComponentPeer::windowIgnoresKeyPresses);
        window=static_cast<HWND>(c.getPeer()->getNativeHandle());
        SetWindowLongPtrW(window,GWL_EXSTYLE,GetWindowLongPtrW(window,GWL_EXSTYLE)|WS_EX_NOACTIVATE|WS_EX_TOOLWINDOW);
        SetWindowSubclass(window,windowProc,1,reinterpret_cast<DWORD_PTR>(this));
        hotkey=RegisterHotKey(window,hotkeyId,MOD_CONTROL|MOD_ALT|MOD_NOREPEAT,'H')!=FALSE;
        c.setAlwaysOnTop(true);
    }
    void setBounds(juce::Rectangle<int> r) override { if (component) component->setBounds(r); }
    juce::Rectangle<int> bounds() const override { return component ? component->getBounds() : juce::Rectangle<int>(); }
    void show(bool visible) override
    {
        component->setVisible(visible);
        if (visible) SetWindowPos(window,HWND_TOPMOST,0,0,0,0,SWP_NOMOVE|SWP_NOSIZE|SWP_NOACTIVATE|SWP_SHOWWINDOW);
    }
    bool hotkeyAvailable() const override { return hotkey; }
    KeyboardState keyboard() override
    {
        const auto t=target();
        if (pending)
        {
            if (!same(*pending,t)) { selectionError="Click your document first"; pending.reset(); }
            else if (t->layout==requested) pending.reset();
            else if (juce::Time::getMillisecondCounterHiRes()>deadline)
            { selectionError="Switch keyboard in editor"; pending.reset(); }
        }
        KeyboardState s;
        if (t)
        {
            s.hasTarget=true;
            const int lang=language(t->layout);
            remembered[lang]=t->layout;
            s.hebrew=lang==0x0d;
            s.english=lang==0x09;
            wchar_t name[100]{};
            GetLocaleInfoW(MAKELCID(LOWORD(reinterpret_cast<uintptr_t>(t->layout)),SORT_DEFAULT),LOCALE_SENGLISHLANGUAGENAME,name,100);
            s.name=s.hebrew ? "Hebrew" : s.english ? "English" : (name[0] ? juce::String(name) : "Other");
        }
        for (auto layout : installed())
        { s.hebrewAvailable |= language(layout)==0x0d; s.englishAvailable |= language(layout)==0x09; }
        return s;
    }
    juce::String insert(char16_t character) override
    {
        if (pending) return "Wait for keyboard change";
        const auto t=target();
        if (!t) return "Click your document first";
        for (int key : {VK_SHIFT,VK_CONTROL,VK_MENU,VK_LWIN,VK_RWIN})
            if (GetAsyncKeyState(key)&0x8000) return "Release modifier keys";
        if (language(t->layout)!=0x0d) return "Select Hebrew keyboard";
        INPUT input[2]{};
        for (auto& event : input)
        { event.type=INPUT_KEYBOARD; event.ki.wScan=character; event.ki.dwFlags=KEYEVENTF_UNICODE; }
        input[1].ki.dwFlags |= KEYEVENTF_KEYUP;
        auto current=target();
        if (!same(*t,current)) return "Click your document first";
        if (language(current->layout)!=0x0d) return "Select Hebrew keyboard";
        return SendInput(2,input,sizeof(INPUT))==2 ? juce::String() : juce::String("Input blocked by Windows");
    }
    juce::String selectKeyboard(bool hebrew) override
    {
        if (pending) return "Wait for keyboard change";
        const auto t=target();
        if (!t) return "Click your document first";
        const int lang=hebrew ? 0x0d : 0x09;
        if (language(t->layout)==lang) return {};
        const auto layouts=installed();
        HKL layout=nullptr;
        for (auto candidate : layouts) if (language(candidate)==lang) { layout=candidate; break; }
        if (remembered.count(lang) && std::find(layouts.begin(),layouts.end(),remembered[lang])!=layouts.end()) layout=remembered[lang];
        if (!layout) return "Keyboard not installed";
        if (!same(*t,target())) return "Click your document first";
        if (!PostMessageW(t->focus,WM_INPUTLANGCHANGEREQUEST,0,reinterpret_cast<LPARAM>(layout))) return "Switch keyboard in editor";
        pending=t;
        requested=layout;
        deadline=juce::Time::getMillisecondCounterHiRes()+1000;
        return {};
    }
    bool selectionPending() const override { return pending.has_value(); }
    juce::String takeSelectionError() override { auto e=selectionError; selectionError.clear(); return e; }
    void requestPermission() override {}
private:
    static LRESULT CALLBACK windowProc(HWND w,UINT msg,WPARAM wp,LPARAM lp,UINT_PTR,DWORD_PTR data)
    {
        auto* self=reinterpret_cast<WindowsPlatform*>(data);
        if (msg==WM_MOUSEACTIVATE) return MA_NOACTIVATE;
        if (msg==WM_HOTKEY && wp==hotkeyId) { self->onToggle(); return 0; }
        return DefSubclassProc(w,msg,wp,lp);
    }
    juce::Component* component=nullptr;
    HWND window=nullptr;
    bool hotkey=false;
    std::function<void()> onToggle;
    std::map<int,HKL> remembered;
    std::optional<Target> pending;
    HKL requested=nullptr;
    double deadline=0;
    juce::String selectionError;
};
}
std::unique_ptr<Platform> makePlatform() { return std::make_unique<WindowsPlatform>(); }
