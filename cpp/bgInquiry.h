#define UNICODE
#define _UNICODE

#include <windows.h>
#include <cstdint>
#include "structs.h"

#define DLL_EXPORTS

#ifdef DLL_EXPORTS
#define DLL_API __declspec(dllexport)
#else
#define DLL_API __declspec(dllimport)
#endif

extern "C"
{
    DLL_API fgAppInfoReturnStruct bgcapture_entry_c();
    DLL_API fgAppInfoReturnStructPOD bgcapture_entry_nonc();
    DLL_API inline uint64_t fnv1a_hash(const char* str,size_t length);
}
