#pragma once
#include <juce_gui_basics/juce_gui_basics.h>
#include <memory>

struct KeyboardState
{
    juce::String name = "click editor";
    bool hasTarget = false;
    bool hebrew = false;
    bool english = false;
    bool hebrewAvailable = false;
    bool englishAvailable = false;
    bool needsPermission = false;
};

// All input sources (palette now, keypad/MIDI later) share this boundary.
// An empty result means the OS accepted the request, not that an editor rendered it.
class Platform
{
public:
    virtual ~Platform() = default;
    virtual void createWindow(juce::Component&, std::function<void()> toggle) = 0;
    virtual void setBounds(juce::Rectangle<int>) = 0;
    virtual juce::Rectangle<int> bounds() const = 0;
    virtual void show(bool visible) = 0;
    virtual bool hotkeyAvailable() const = 0;
    virtual KeyboardState keyboard() = 0;
    virtual juce::String insert(char16_t character) = 0;
    virtual juce::String selectKeyboard(bool hebrew) = 0;
    virtual bool selectionPending() const = 0;
    virtual juce::String takeSelectionError() = 0;
    virtual void requestPermission() = 0;
};
std::unique_ptr<Platform> makePlatform();
