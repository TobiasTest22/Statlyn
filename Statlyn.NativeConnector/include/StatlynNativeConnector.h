#pragma once

#include <cstdint>

#if defined(_WIN32)
#if defined(STATLYN_NATIVE_CONNECTOR_EXPORTS)
#define STATLYN_API __declspec(dllexport)
#else
#define STATLYN_API __declspec(dllimport)
#endif
#else
#define STATLYN_API
#endif

extern "C"
{
    enum StatlynStatus
    {
        STATLYN_STATUS_OK = 0,
        STATLYN_STATUS_NOT_FOUND = 1,
        STATLYN_STATUS_ACCESS_DENIED = 2,
        STATLYN_STATUS_UNSUPPORTED_BUILD = 3,
        STATLYN_STATUS_INVALID_ARGUMENT = 4,
        STATLYN_STATUS_INTERNAL_ERROR = 5
    };

    struct StatlynProcessInfo
    {
        uint32_t processId;
        wchar_t executablePath[520];
        wchar_t productVersion[128];
        wchar_t architecture[32];
        uintptr_t moduleBaseAddress;
        int detected;
        int readOnlyAccess;
    };

    STATLYN_API int Statlyn_DetectFM26(StatlynProcessInfo* processInfo);
    STATLYN_API int Statlyn_GetProcessInfo(StatlynProcessInfo* processInfo);
    STATLYN_API int Statlyn_OpenReadOnly(uint32_t processId);
    STATLYN_API void Statlyn_Close();
    STATLYN_API int Statlyn_ValidateBuild();
    STATLYN_API int Statlyn_ReadSnapshot(char* buffer, int bufferLength);
    STATLYN_API int Statlyn_GetLastError(char* buffer, int bufferLength);
    STATLYN_API int Statlyn_GetDiagnostics(char* buffer, int bufferLength);
    STATLYN_API int Statlyn_GetConnectorVersion(char* buffer, int bufferLength);
}
