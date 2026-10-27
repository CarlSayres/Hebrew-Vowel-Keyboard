#pragma once
#include "Platform.h"
#include "Catalog.h"

class Palette final : public juce::Component, private juce::Timer
{
public:
    explicit Palette(bool presentWindow = true);
    ~Palette() override;
    void paint(juce::Graphics&) override;
    void resized() override;
    void mouseDown(const juce::MouseEvent&) override;
    void mouseDrag(const juce::MouseEvent&) override;
    void toggleVisibility();
    void userTriedToCloseWindow() override { juce::JUCEApplication::getInstance()->systemRequestedQuit(); }
    bool renderPreviews(const juce::File& directory);
private:
    void setPage(bool teamim);
    void notice(const juce::String&);
    void timerCallback() override;
    std::unique_ptr<Platform> platform;
    juce::TextButton niqqud { "Niqqud" }, teamim { juce::String::fromUTF8("Te’amim") };
    juce::TextButton hebrew { juce::String::fromUTF8("עברית") }, english { "English" }, close { "x" };
    juce::TextButton permission { "Allow access" };
    std::vector<std::unique_ptr<juce::Button>> marks;
    juce::Rectangle<int> dragBounds;
    bool teamimPage = false, visible = true;
    juce::String hint, keyboardText;
    bool keyboardHebrew = false;
    double noticeUntil = 0;
};
