#include "Palette.h"

class HebrewMarksApplication final : public juce::JUCEApplication
{
public:
    const juce::String getApplicationName() override { return "Hebrew Marks"; }
    const juce::String getApplicationVersion() override { return "0.2.0"; }
    bool moreThanOneInstanceAllowed() override { return false; }
    void initialise(const juce::String& commandLine) override
    {
        auto args=juce::StringArray::fromTokens(commandLine,true);
        args.trim();
        if (args.size()==2 && args[0]=="--render")
        {
            palette=std::make_unique<Palette>(false);
            setApplicationReturnValue(palette->renderPreviews(juce::File(args[1].unquoted())) ? 0 : 1);
            quit();
            return;
        }
        palette = std::make_unique<Palette>();
    }
    void shutdown() override { palette.reset(); }
    void systemRequestedQuit() override { quit(); }
    void anotherInstanceStarted(const juce::String&) override {}
private:
    std::unique_ptr<Palette> palette;
};
START_JUCE_APPLICATION(HebrewMarksApplication)
