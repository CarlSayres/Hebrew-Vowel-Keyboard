cmake_minimum_required(VERSION 3.24)
project(HebrewMarks VERSION 0.2.0 LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
# Bundle the Microsoft C/C++ runtime into the executable, too.
set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
if(APPLE)
    enable_language(OBJCXX)
    set(CMAKE_OBJCXX_STANDARD 17)
    set(CMAKE_OBJCXX_STANDARD_REQUIRED ON)
    set(CMAKE_OSX_DEPLOYMENT_TARGET "11.0" CACHE STRING "Minimum macOS version")
endif()

include(FetchContent)
FetchContent_Declare(JUCE
    URL https://github.com/juce-framework/JUCE/archive/3fb5cbc0340758596dac39f7c8de47d562b4896e.zip
    URL_HASH SHA256=47722c9e64f5af35b99c6d5f9605a2213e41858ff6ef1f5b9a9747b1fd310de3
    DOWNLOAD_EXTRACT_TIMESTAMP TRUE)
FetchContent_MakeAvailable(JUCE)

juce_add_gui_app(HebrewMarks
    PRODUCT_NAME "Hebrew Marks"
    COMPANY_NAME "Hebrew Marks"
    BUNDLE_ID "org.hebrewmarks.palette"
    NEEDS_WEB_BROWSER FALSE
    NEEDS_CURL FALSE
    PLIST_TO_MERGE "<plist><dict><key>LSUIElement</key><true/><key>NSHighResolutionCapable</key><true/></dict></plist>")
target_sources(HebrewMarks PRIVATE Source/Main.cpp Source/Palette.cpp Source/Catalog.cpp)
if(WIN32)
    target_sources(HebrewMarks PRIVATE Source/PlatformWindows.cpp)
    target_compile_options(HebrewMarks PRIVATE /utf-8)
    target_link_libraries(HebrewMarks PRIVATE comctl32)
elseif(APPLE)
    target_sources(HebrewMarks PRIVATE Source/PlatformMac.mm)
    target_link_libraries(HebrewMarks PRIVATE "-framework Carbon" "-framework ApplicationServices")
else()
    message(FATAL_ERROR "Hebrew Marks supports Windows and macOS.")
endif()
target_compile_definitions(HebrewMarks PRIVATE
    JUCE_WEB_BROWSER=0 JUCE_USE_CURL=0 JUCE_MODAL_LOOPS_PERMITTED=0)
target_link_libraries(HebrewMarks PRIVATE juce::juce_gui_basics
    juce::juce_recommended_config_flags juce::juce_recommended_lto_flags)

enable_testing()
add_executable(CatalogTests Tests/CatalogTests.cpp Source/Catalog.cpp)
target_include_directories(CatalogTests PRIVATE Source)
if(MSVC)
    target_compile_options(CatalogTests PRIVATE /utf-8)
endif()
add_test(NAME Catalog COMMAND CatalogTests)
