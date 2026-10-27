#include "Palette.h"
#include "MarkPreview.h"

namespace
{
const juce::Colour ink(0xff162b3b), teal(0xff218675), pale(0xffe5f3f0), amber(0xffffce83);
constexpr int headerHeight = 112, cellWidth = 96, cellHeight = 82;

class MarkButton final : public juce::Button
{
public:
    explicit MarkButton(const Mark& m) : Button(juce::String::fromUTF8(m.name)), mark(m), preview(m.character)
    {
        setWantsKeyboardFocus(false);
        setMouseClickGrabsKeyboardFocus(false);
        setTitle(juce::String::fromUTF8(m.name));
    }
    void paintButton(juce::Graphics& g, bool over, bool down) override
    {
        const auto r = getLocalBounds().toFloat().reduced(0.5f);
        g.setColour(down ? juce::Colour(0xffc9e6df) : over ? pale : juce::Colours::white);
        g.fillRoundedRectangle(r, 6);
        g.setColour(over ? teal : juce::Colour(0xffdce3e8));
        g.drawRoundedRectangle(r, 6, 1);
        g.setColour(ink);
        preview.draw(g,getWidth()/2.0f);
        g.setFont(juce::Font(juce::FontOptions(10.0f).withStyle("Bold")));
        g.drawFittedText(juce::String::fromUTF8(mark.name), 2, 46, getWidth()-4, 13, juce::Justification::centred, 1);
        g.setColour(juce::Colour(0xff667785));
        g.setFont(juce::Font(juce::FontOptions(11.0f)));
        g.drawText(juce::String::fromUTF8(mark.hebrew), 2, 60, getWidth()-4, 14, juce::Justification::centred, false);
    }
    const Mark& mark;
    const MarkPreview preview;
};
}

Palette::Palette(bool presentWindow) : platform(makePlatform())
{
    setName("Hebrew Marks");
    setOpaque(true);
    setWantsKeyboardFocus(false);
    setMouseClickGrabsKeyboardFocus(false);
    for (auto* b : { &niqqud, &teamim, &hebrew, &english, &close, &permission })
    {
        addAndMakeVisible(*b);
        b->setWantsKeyboardFocus(false);
        b->setMouseClickGrabsKeyboardFocus(false);
        b->setColour(juce::TextButton::buttonColourId, juce::Colours::white);
        b->setColour(juce::TextButton::textColourOffId, ink);
    }
    permission.setVisible(false);
    niqqud.onClick = [this] { setPage(false); };
    teamim.onClick = [this] { setPage(true); };
    hebrew.onClick = [this] { notice(platform->selectKeyboard(true)); timerCallback(); };
    english.onClick = [this] { notice(platform->selectKeyboard(false)); timerCallback(); };
    close.onClick = [] { juce::JUCEApplication::getInstance()->systemRequestedQuit(); };
    permission.onClick = [this] { platform->requestPermission(); };
    setPage(false);
    if (!presentWindow) return;
    platform->createWindow(*this, [this] { toggleVisibility(); });
    auto area = juce::Desktop::getInstance().getDisplays().getPrimaryDisplay()->userArea;
    platform->setBounds({area.getRight()-getWidth()-24, area.getY()+24, getWidth(), getHeight()});
    platform->show(true);
    timerCallback();
    startTimer(250);
}

Palette::~Palette()
{
    stopTimer();
    // The native host must release its view before Component is destroyed.
    platform.reset();
    removeFromDesktop();
}

void Palette::setPage(bool isTeamim)
{
    auto oldBounds = platform->bounds();
    teamimPage = isTeamim;
    marks.clear();
    const auto& rows = teamimPage ? teamimRows() : niqqudRows();
    for (const auto& row : rows)
        for (const auto& id : row)
        {
            const auto& mark = findMark(id);
            auto button = std::make_unique<MarkButton>(mark);
            button->onClick = [this, character=mark.character]
            { notice(platform->insert(character)); timerCallback(); };
            addAndMakeVisible(*button);
            marks.push_back(std::move(button));
        }
    setSize((teamimPage ? 6 : 3)*cellWidth+18, headerHeight+12+static_cast<int>(rows.size())*cellHeight);
    if (!oldBounds.isEmpty())
        platform->setBounds({oldBounds.getRight()-getWidth(),oldBounds.getY(),getWidth(),getHeight()});
    niqqud.setColour(juce::TextButton::buttonColourId, teamimPage ? juce::Colours::white : pale);
    teamim.setColour(juce::TextButton::buttonColourId, teamimPage ? pale : juce::Colours::white);
    resized();
    repaint();
}

void Palette::resized()
{
    close.setBounds(getWidth()-36, 9, 26, 24);
    niqqud.setBounds(getWidth()-147, 43, 63, 27);
    teamim.setBounds(getWidth()-79, 43, 69, 27);
    hebrew.setBounds(getWidth()-139, 79, 59, 24);
    english.setBounds(getWidth()-75, 79, 65, 24);
    permission.setBounds(10, 79, 132, 24);
    int index=0, y=headerHeight+6;
    for (const auto& row : teamimPage ? teamimRows() : niqqudRows())
    {
        int x=getWidth()-9-cellWidth;
        for (size_t column=0; column<row.size(); ++column)
        {
            marks[static_cast<size_t>(index++)]->setBounds(x+2,y+2,92,78);
            x-=cellWidth;
        }
        y+=cellHeight;
    }
}

void Palette::paint(juce::Graphics& g)
{
    g.fillAll(juce::Colour(0xfff7f8fa));
    g.setColour(juce::Colour(0xff172f40));
    g.fillRect(0,0,getWidth(),headerHeight);
    g.setColour(juce::Colours::white);
    g.setFont(juce::Font(juce::FontOptions(18.0f)));
    g.drawText(juce::String::fromUTF8("Niqqud & Te’amim"),12,7,getWidth()-54,27,juce::Justification::centredLeft);
    const bool showingNotice = juce::Time::getMillisecondCounterHiRes() < noticeUntil;
    g.setColour(showingNotice ? amber : juce::Colour(0xffb2dbd2));
    g.setFont(juce::Font(juce::FontOptions(10.0f)));
    juce::String normal = platform->hotkeyAvailable() ? "Ctrl-Alt-H to show or hide" : "Shortcut unavailable";
    g.drawFittedText(showingNotice ? hint : normal, 12,40,getWidth()-166,34,juce::Justification::centredLeft,2);
    if (!permission.isVisible())
    {
        g.setColour(keyboardHebrew ? juce::Colour(0xffb2dbd2) : amber);
        g.setFont(juce::Font(juce::FontOptions(11.0f)));
        g.drawFittedText("Keyboard: " + keyboardText,12,79,getWidth()-157,24,juce::Justification::centredLeft,1);
    }
}

void Palette::notice(const juce::String& message)
{
    hint = message;
    noticeUntil = message.isEmpty() ? 0 : juce::Time::getMillisecondCounterHiRes()+6000;
    repaint();
}

void Palette::timerCallback()
{
    const auto state=platform->keyboard();
    keyboardText=state.name;
    keyboardHebrew=state.hebrew;
    permission.setVisible(state.needsPermission);
    const bool ready=state.hasTarget && !platform->selectionPending();
    hebrew.setEnabled(ready && state.hebrewAvailable);
    english.setEnabled(ready && state.englishAvailable);
    hebrew.setColour(juce::TextButton::buttonColourId, state.hebrew ? pale : juce::Colours::white);
    english.setColour(juce::TextButton::buttonColourId, state.english ? pale : juce::Colours::white);
    const auto error=platform->takeSelectionError();
    if (error.isNotEmpty()) notice(error);
    repaint();
}

void Palette::mouseDown(const juce::MouseEvent& e)
{
    if (e.y<headerHeight) dragBounds=platform->bounds();
}
void Palette::mouseDrag(const juce::MouseEvent& e)
{
    if (e.getMouseDownY()<headerHeight)
        platform->setBounds(dragBounds.translated(e.getDistanceFromDragStartX(), e.getDistanceFromDragStartY()));
}
void Palette::toggleVisibility()
{
    visible=!visible;
    platform->show(visible);
}

bool Palette::renderPreviews(const juce::File& directory)
{
    if (!directory.createDirectory()) return false;
    for (bool page : {false,true})
    {
        setPage(page);
        auto output=directory.getChildFile(page ? "teamim.png" : "niqqud.png").createOutputStream();
        if (!output) return false;
        output->setPosition(0);
        output->truncate();
        if (!juce::PNGImageFormat().writeImageToStream(createComponentSnapshot(getLocalBounds()),*output)) return false;
    }
    return true;
}
