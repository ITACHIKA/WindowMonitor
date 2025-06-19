#include "bgInquiry.h"
#include <windows.h>
#include <psapi.h>
#include <tchar.h>
#include <iostream>
#include <fstream>
#include <mutex>
#include <cstdint>
#include <cstring>
#include <shlwapi.h>
#include <filesystem>
#include <gdiplus.h>
#include <combaseapi.h>

static std::mutex g_hIconMutex;
static HICON g_hIcon = NULL;
Gdiplus::Bitmap *g_hPng = nullptr;
ULONG_PTR gdiPlusToken = 0;
static CLSID pBmpCLSID;
static bool pBmpCLSIDInit = false;

int GetEncoderClsid(const WCHAR *format, CLSID *pClsid)
{
    UINT num = 0;  // number of image encoders
    UINT size = 0; // size of the image encoder array in bytes

    Gdiplus::ImageCodecInfo *pImageCodecInfo = nullptr;

    Gdiplus::GetImageEncodersSize(&num, &size);
    if (size == 0)
        return -1; // failure

    pImageCodecInfo = (Gdiplus::ImageCodecInfo *)(malloc(size));
    if (pImageCodecInfo == nullptr)
        return -1; // failure

    GetImageEncoders(num, size, pImageCodecInfo);

    for (UINT j = 0; j < num; ++j)
    {
        if (wcscmp(pImageCodecInfo[j].MimeType, format) == 0)
        {
            *pClsid = pImageCodecInfo[j].Clsid;
            free(pImageCodecInfo);
            return j; // success
        }
    }

    free(pImageCodecInfo);
    return -1; // failure
}

inline uint64_t fnv1a_hash(const char *str, size_t length) noexcept
{
    const uint64_t FNV_OFFSET_BASIS = 14695981039346656037ULL;
    const uint64_t FNV_PRIME = 1099511628211ULL;

    uint64_t hash = FNV_OFFSET_BASIS;
    for (size_t i = 0; i < length; i++)
    {
        hash ^= static_cast<unsigned char>(*(str + i));
        hash *= FNV_PRIME;
    }
    return hash;
}

void convTCHAR2UTF8(const TCHAR *org, std::string *target)
{
    auto len = WideCharToMultiByte(CP_UTF8, 0, org, -1, nullptr, 0, nullptr, nullptr);
    target->resize(len);
    WideCharToMultiByte(CP_UTF8, 0, org, -1, &((*target)[0]), len, nullptr, nullptr);
}

fgAppInfoStruct LogActiveWindow(HWND hwnd)
{
    TCHAR title[256];
    GetWindowText(hwnd, title, sizeof(title) / sizeof(TCHAR));
    DWORD pid;
    GetWindowThreadProcessId(hwnd, &pid);
    static TCHAR exePath[MAX_PATH];
    TCHAR *appnameW = nullptr; // appname with wide char
    std::string appname;
    uint64_t apphash;
    HANDLE hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, FALSE, pid);
    if (hProcess)
    {
        if (GetModuleFileNameEx(hProcess, NULL, exePath, MAX_PATH))
        {
            std::string titleUTF8;
            std::string exePathUTF8;
            convTCHAR2UTF8(title, &titleUTF8);
            convTCHAR2UTF8(exePath, &exePathUTF8);
            auto lastPath = exePathUTF8.rfind("\\");
            auto lastPathUTF16 = StrRChr(exePath, NULL, _T('\\'));
            appnameW = lastPathUTF16 ? lastPathUTF16 + 1 : exePath;
            appname = exePathUTF8.substr(lastPath + 1);
            apphash = fnv1a_hash(exePathUTF8.c_str(), exePathUTF8.length());
            std::cout << "Window change:\n";
            std::cout << "  Title " << titleUTF8 << std::endl;
            std::cout << "  Path  " << exePathUTF8 << std::endl;
            std::cout << "  Hash: " << apphash << std::endl;
            std::cout << "  Program: " << appname << std::endl;
            // std::wofstream logFile("window_log.txt", std::ios::app);
            // logFile << "Change title: " << title << "\n  Path: " << exePath << L"\n\n";
        }
        CloseHandle(hProcess);
        auto path = std::string("cache/") + std::to_string(apphash);
        std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>> converter;
        auto pathW = converter.from_bytes(path);
        if (std::filesystem::exists(path))
        {
            std::cout << "cache hit" << std::endl;
            // delete g_hPng;
            // g_hPng = new Gdiplus::Bitmap(&(pathW[0]));
            // if (!g_hPng)
            // {
            //     std::cout << "cache read error" << std::endl;
            //     DWORD errC = GetLastError();
            //     std::cout << errC << std::endl;
            // }
        }
        else
        {
            std::cout << "cache not found" << std::endl;
            HMODULE hModule = LoadLibraryEx(exePath, NULL, LOAD_LIBRARY_AS_DATAFILE);
            auto callback = [](HMODULE h, LPCWSTR type, LPWSTR name, LONG_PTR param)
            {
                EnumIconContext *context = reinterpret_cast<EnumIconContext *>(param);

                HRSRC hRes = FindResource(h, name, RT_GROUP_ICON);
                if (!hRes)
                    return TRUE;

                HGLOBAL hData = LoadResource(h, hRes);
                if (!hData)
                    return TRUE;

                BYTE *iconGroup = (BYTE *)LockResource(hData);
                int iconId = LookupIconIdFromDirectoryEx(iconGroup, TRUE, 256, 256, LR_DEFAULTCOLOR);

                HRSRC hIconRes = FindResource(h, MAKEINTRESOURCE(iconId), RT_ICON);
                if (!hIconRes)
                    return TRUE;

                HGLOBAL hIconData = LoadResource(h, hIconRes);
                if (!hIconData)
                    return TRUE;

                BYTE *iconBytes = (BYTE *)LockResource(hIconData);
                DWORD size = SizeofResource(h, hIconRes);

                HICON hIcon = CreateIconFromResourceEx(iconBytes, size, TRUE, 0x00030000, 256, 256, LR_DEFAULTCOLOR);

                if (hIcon)
                {
                    ICONINFO ii;
                    if (GetIconInfo(hIcon, &ii))
                    {
                        BITMAP bm;
                        if (GetObject(ii.hbmColor, sizeof(bm), &bm))
                        {
                            auto iconArea = bm.bmWidth * bm.bmHeight;
                            if (iconArea > context->maxSize)
                            {
                                DestroyIcon(context->bestIcon);
                                context->bestIcon = hIcon;
                                context->maxSize = iconArea;
                            }
                            else
                            {
                                DestroyIcon(hIcon);
                            }
                        }
                        DeleteObject(ii.hbmColor);
                        DeleteObject(ii.hbmMask);
                    }
                }
                return TRUE;
            };
            EnumIconContext context;
            EnumResourceNames(hModule, RT_GROUP_ICON, (ENUMRESNAMEPROCW)callback, (LONG_PTR)&context);
            FreeLibrary(hModule);
            std::lock_guard<std::mutex> lock(g_hIconMutex);
            if (g_hIcon)
                DestroyIcon(g_hIcon);
            g_hIcon = context.bestIcon;
            // convert from HICON to HBITMAP
            Gdiplus::Bitmap *pBitMap = Gdiplus::Bitmap::FromHICON(g_hIcon);
            if (!pBitMap)
                std::cout << "Err: failed to create BMP" << std::endl;
            if (!pBmpCLSIDInit)
            {
                if (GetEncoderClsid(L"image/png", &pBmpCLSID) < 0)
                {
                    MessageBox(NULL, L"can't load encoder", L"Error", MB_OK);
                    for (;;)
                        ;
                }
                pBmpCLSIDInit = true;
            }
            auto w = pBitMap->GetWidth();
            auto h = pBitMap->GetHeight();
            Gdiplus::Bitmap *pAlphaBitmap = new Gdiplus::Bitmap(w, h, PixelFormat32bppARGB);
            Gdiplus::Graphics g(pAlphaBitmap);
            g.Clear(Gdiplus::Color(0x00000000)); // ARGB
            g.DrawImage(pBitMap, 0, 0, w, h);
            if (pAlphaBitmap->Save(pathW.c_str(), &pBmpCLSID, nullptr) != Gdiplus::Ok)
            {
                std::cout << "failed to save bmp" << std::endl;
            }
            delete pBitMap;
            delete g_hPng;
            g_hPng = pAlphaBitmap;
        }
        char *appnamePointer = new char[appname.size() + 1];
        std::strcpy(appnamePointer, appname.c_str());
        return fgAppInfoStruct(appnamePointer, apphash, appnameW, exePath);
    }
    else
    {
        MessageBox(NULL, L"can't open process", L"Error", MB_OK);
        return fgAppInfoStruct();
    }
}

/*
 * Returns a fgAppInfoReturnStruct containing info about captured window. C style.
 *
 * HICON is released by the function on next call. appnameW* will be updated on next window update. appname* is C++ managed char*.
 *
 * To prevent undefined behavior, users should make copies of the above variable, or ensure the variables are used within their life cycle.
 */
fgAppInfoReturnStruct bgcapture_entry_c()
{
    static HWND prevWindow = NULL;
    static HWND selfWindow = FindWindow(_T("WindowLogMainWND"), NULL);
    HWND curWindow = GetForegroundWindow();
    if (curWindow != NULL && curWindow != prevWindow && curWindow != selfWindow)
    {
        prevWindow = curWindow;
        return fgAppInfoReturnStruct(NORMAL, LogActiveWindow(curWindow));
    }
    return fgAppInfoReturnStruct(NOUPDATE, fgAppInfoStruct());
}
/*For non c language use, returns POD data structure*/
fgAppInfoReturnStructPOD bgcapture_entry_nonc()
{
    static HWND prevWindow = NULL;
    static HWND selfWindow = FindWindow(_T("WindowLogMainWND"), NULL);
    HWND curWindow = GetForegroundWindow();
    if (curWindow != NULL && curWindow != prevWindow && curWindow != selfWindow)
    {
        prevWindow = curWindow;
        auto res = LogActiveWindow(curWindow);
        if (res.appName && res.appnameW && res.appPath)
        {
            fgAppInfoReturnStructPOD retPOD;
            retPOD.appHash = res.appHash;
            retPOD.returnState = NORMAL;
            retPOD.appName = (char *)CoTaskMemAlloc(sizeof(char) * (std::strlen(res.appName) + 1));
            std::strcpy(retPOD.appName, res.appName);
            retPOD.appnameW = (TCHAR *)CoTaskMemAlloc(sizeof(TCHAR) * (std::wcslen(res.appnameW) + 1));
            std::wcscpy(retPOD.appnameW, res.appnameW);
            retPOD.appPath = (TCHAR *)CoTaskMemAlloc(sizeof(TCHAR) * (std::wcslen(res.appPath) + 1));
            std::wcscpy(retPOD.appPath, res.appPath);
            return retPOD;
        }
    }
    fgAppInfoReturnStructPOD retPOD;
    retPOD.returnState = NOUPDATE;
    return retPOD;
}

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpReserved)
{
    switch (fdwReason)
    {
    case DLL_PROCESS_ATTACH:
    {
        if (!std::filesystem::exists("cache"))
        {
            std::filesystem::create_directory("cache");
        }
        Gdiplus::GdiplusStartupInput gdiPlusStartupInput;
        if (Gdiplus::GdiplusStartup(&gdiPlusToken, &gdiPlusStartupInput, nullptr) != Gdiplus::Ok)
        {
            std::cerr << "Failed to initialize GDI+." << std::endl;
        }
        break;
    }
    case DLL_PROCESS_DETACH:
    {
        if (g_hIcon != NULL)
        {
            DestroyIcon(g_hIcon);
            g_hIcon = NULL;
        }
        if (g_hPng != nullptr)
        {
            delete g_hPng;
            g_hPng = nullptr;
        }
        if (gdiPlusToken != 0)
        {
            Gdiplus::GdiplusShutdown(gdiPlusToken);
            gdiPlusToken = 0;
        }
        break;
    }
    default:
        break;
    }
    return TRUE;
}