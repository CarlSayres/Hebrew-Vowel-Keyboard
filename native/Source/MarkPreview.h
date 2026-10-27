#pragma once
#include "Catalog.h"
#include <juce_gui_basics/juce_gui_basics.h>

// Combining marks have no advance width. Letting a text layout align a run
// containing U+25CC can align the circle and accent independently. Place the
// actual font outlines by their ink bounds instead; this is display-only.
class MarkPreview
{
public:
    explicit MarkPreview(char16_t character)
    {
        const juce::Font font(juce::FontOptions("Times New Roman", 32.0f, juce::Font::plain));
        auto typeface=font.getTypefacePtr();
        auto glyph=typeface->getNominalGlyphForCodepoint(character);
        if (!glyph)
        {
            if (auto fallback=typeface->createSystemFallback(juce::String::charToString(character), "he"))
            { typeface=fallback; glyph=typeface->getNominalGlyphForCodepoint(character); }
        }
        if (glyph) typeface->getOutlineForGlyph(static_cast<int>(*glyph), mark);
        mark.applyTransform(juce::AffineTransform::scale(32.0f));

        const bool punctuation=isPunctuation(character);
        if (!punctuation)
        {
            // A fixed dotted circle keeps the base identical across both OSes.
            for (int i=0; i<16; ++i)
            {
                const auto angle=juce::MathConstants<float>::twoPi*static_cast<float>(i)/16.0f;
                circle.addEllipse(8.0f*std::cos(angle)-0.7f, 23.0f+8.0f*std::sin(angle)-0.7f, 1.4f,1.4f);
            }
        }
        auto box=mark.getBounds();
        if (box.isEmpty()) return;
        const float maxHeight=punctuation ? 28.0f : 10.0f;
        const float scale=juce::jmin(1.0f, maxHeight/box.getHeight(), 28.0f/box.getWidth());
        mark.applyTransform(juce::AffineTransform::scale(scale));
        box=mark.getBounds();

        float x=0.0f, y=23.0f-box.getHeight()/2.0f;
        if (!punctuation && character!=0x05bc)
        {
            if (isAbove(character)) y=12.0f-box.getHeight();
            else y=34.0f;
            if (character==0x05c1) x=6.0f; // shin: upper right
            if (character==0x05c2 || character==0x05b9 || character==0x05ba) x=-6.0f;
            if (character==0x059a || character==0x05ad) x=-11.0f; // yetiv/dehi: below left
            if (character==0x05ae) x=-6.0f; // zarqa: above left
        }
        mark.applyTransform(juce::AffineTransform::translation(x-box.getCentreX(),y-box.getY()));
    }

    void draw(juce::Graphics& g, float centreX) const
    {
        const auto transform=juce::AffineTransform::translation(centreX,0);
        g.fillPath(circle,transform);
        g.fillPath(mark,transform);
    }

private:
    static bool isAbove(char16_t c)
    {
        switch (c)
        {
            case 0x0592: case 0x0593: case 0x0594: case 0x0595: case 0x0597:
            case 0x0598: case 0x0599: case 0x059c: case 0x059d: case 0x059e:
            case 0x059f: case 0x05a0: case 0x05a1: case 0x05a8: case 0x05a9:
            case 0x05ab: case 0x05ac: case 0x05ae: case 0x05af:
            case 0x05b9: case 0x05ba: case 0x05bf: case 0x05c1: case 0x05c2:
                return true;
            default: return false;
        }
    }
    juce::Path circle, mark;
};
