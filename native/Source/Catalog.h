#pragma once
#include <cstdint>
#include <string>
#include <vector>

struct Mark
{
    const char* id;
    const char* name;
    const char* hebrew;
    char16_t character;
    bool niqqud;
};
const std::vector<Mark>& catalog();
const Mark& findMark(const std::string& id);
using Rows = std::vector<std::vector<std::string>>;
const Rows& niqqudRows();
const Rows& teamimRows();
bool isPunctuation(char16_t character);
