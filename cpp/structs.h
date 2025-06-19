#pragma once

#include <windows.h>
#include <cstdint>
#include <iostream>
#include <gdiplus.h>

enum hiconState
{
    NORMAL,
    NOUPDATE
};

enum callerType
{
    CPP,
    OTHER
};

/**
 * fgAppInfoStruct for storing current app window info captured.
 *
 * Do not free pointers.
 *
 * Copy functions are deleted. Move only.
 *
 * @param g_hPng - Handle to PNG icon of window
 * @param appName - name of the application that runs the window (UTF8) - pointer is C++ managed
 * @param appNameW - name of the application that runs the window, wide char style - pointer to static TCHAR[]
 * @param appHash - hash of the application's path
 *
 */
struct fgAppInfoStruct
{
    // HICON hIcon = NULL; // currently unused
    // Gdiplus::Bitmap *g_hPng = nullptr;
    uint64_t appHash = 0;
    const char *appName = nullptr;
    const TCHAR *appnameW = nullptr;
    const TCHAR *appPath = nullptr;
    fgAppInfoStruct() noexcept {};
    // fgAppInfoStruct(HICON hI,Gdiplus::Bitmap *hP, const char *aN, uint64_t aH, const TCHAR *aW) noexcept :hIcon(hI),g_hPng(hP), appName(aN), appnameW(aW), appHash(aH) {};
    // fgAppInfoStruct(Gdiplus::Bitmap *hP, const char *aN, uint64_t aH, const TCHAR *aW) noexcept :g_hPng(hP), appName(aN), appnameW(aW), appHash(aH) {};
    fgAppInfoStruct(const char *aN, uint64_t aH, const TCHAR *aW, const TCHAR* aP) noexcept :appHash(aH), appName(aN), appnameW(aW),appPath(aP) {};
    fgAppInfoStruct &operator=(const fgAppInfoStruct &other) = delete;
    fgAppInfoStruct(const fgAppInfoStruct &other) = delete;
    fgAppInfoStruct &operator=(fgAppInfoStruct &&other) noexcept
    {
        if (this != &other)
        {
            delete[] this->appName;
            this->appName = other.appName;
            // this->hIcon = other.hIcon;
            // this->g_hPng = other.g_hPng;
            this->appHash = other.appHash;
            this->appnameW = other.appnameW;
            other.appName = nullptr;
        }
        return *this;
    }
    fgAppInfoStruct(fgAppInfoStruct &&other) noexcept
    {
        delete[] this->appName;
        this->appName = other.appName;
        // this->hIcon = other.hIcon;
        // this->g_hPng = other.g_hPng;
        this->appHash = other.appHash;
        this->appnameW = other.appnameW;
        other.appName = nullptr;
    }
    ~fgAppInfoStruct() noexcept
    {
        delete[] appName;
    }
};

struct fgAppInfoReturnStructPOD
{
    hiconState returnState;
    uint64_t appHash=0;
    char *appName = nullptr;
    TCHAR *appnameW = nullptr;
    TCHAR *appPath = nullptr;
};

struct fgAppInfoReturnStruct
{
    hiconState returnState;
    fgAppInfoStruct fgAppInfo;
    fgAppInfoReturnStruct() noexcept {};
    fgAppInfoReturnStruct(hiconState r, fgAppInfoStruct &&f) noexcept : returnState(r), fgAppInfo(std::move(f)) {};
};

struct EnumIconContext
{
    int maxSize = 0;
    HICON bestIcon = nullptr;
};