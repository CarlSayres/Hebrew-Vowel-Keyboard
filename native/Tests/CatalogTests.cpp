#include "Catalog.h"
#include <iostream>
#include <set>
#include <stdexcept>

void require(bool ok, const char* message) { if (!ok) throw std::runtime_error(message); }
int main()
{
    try
    {
        require(catalog().size()==52,"Expected 52 unique marks");
        std::set<std::string> ids;
        std::set<char16_t> characters;
        for (const auto& m : catalog())
        { require(ids.insert(m.id).second,"Duplicate ID"); require(characters.insert(m.character).second,"Duplicate character"); }
        for (char16_t c=0x0591; c<=0x05af; ++c) require(characters.count(c)==1,"Missing accent");
        require(findMark("dagesh").character==0x05bc,"Mappiq must share dagesh");
        require(findMark("zinor").character==0x05ae,"Zarqa mapping changed");
        require(findMark("zarqa").character==0x0598,"Tsinorit mapping changed");
        require(findMark("mahapakh").character==0x05a4,"Mapach mapping changed");
        require(isPunctuation(0x05be) && isPunctuation(0x05c0) && isPunctuation(0x05c3) && !isPunctuation(0x05bc),"Punctuation previews");
        std::set<std::string> displayed;
        int count=0;
        for (auto* rows : { &niqqudRows(), &teamimRows() })
            for (const auto& row : *rows)
                for (const auto& id : row) { findMark(id); displayed.insert(id); ++count; }
        require(displayed==ids && count==54,"All marks plus intentional repeats must be displayed");
        require(niqqudRows().size()==7 && teamimRows().size()==6,"Page sizes changed");
        require(niqqudRows()[4]==std::vector<std::string>({"shin-dot","sin-dot","holam"}),"Dot row order");
        require(niqqudRows()[6]==std::vector<std::string>({"rafe","sof-pasuq","paseq"}),"Final niqqud row order");
        require(teamimRows()[3].back()=="shalshelet","Shalshelet placement");
        std::cout << "PASS: 52 code points, 54 buttons, complete accents, punctuation and RTL row mappings\n";
        return 0;
    }
    catch (const std::exception& e) { std::cerr << e.what() << '\n'; return 1; }
}
