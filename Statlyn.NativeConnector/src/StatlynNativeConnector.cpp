#include "StatlynNativeConnector.h"

#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#include <TlHelp32.h>
#include <winver.h>

#include <algorithm>
#include <cctype>
#include <cstdio>
#include <cstring>
#include <string>
#include <vector>

namespace
{
    HANDLE g_processHandle = nullptr;
    DWORD g_processId = 0;
    std::string g_lastError;
    StatlynProcessInfo g_lastProcessInfo = {};

    void SetLastErrorText(const std::string& text)
    {
        g_lastError = text;
    }

    void CopyWide(wchar_t* target, size_t targetLength, const std::wstring& value)
    {
        if (target == nullptr || targetLength == 0)
        {
            return;
        }

        wcsncpy_s(target, targetLength, value.c_str(), _TRUNCATE);
    }

    int CopyUtf8(char* target, int targetLength, const std::string& value)
    {
        if (target == nullptr || targetLength <= 0)
        {
            return STATLYN_STATUS_INVALID_ARGUMENT;
        }

        strncpy_s(target, static_cast<size_t>(targetLength), value.c_str(), _TRUNCATE);
        return STATLYN_STATUS_OK;
    }

    std::wstring QueryProcessPath(HANDLE processHandle)
    {
        std::vector<wchar_t> buffer(520);
        DWORD size = static_cast<DWORD>(buffer.size());
        if (!QueryFullProcessImageNameW(processHandle, 0, buffer.data(), &size))
        {
            return L"";
        }

        return std::wstring(buffer.data(), size);
    }

    std::wstring QueryProductVersion(const std::wstring& path)
    {
        DWORD handle = 0;
        const DWORD size = GetFileVersionInfoSizeW(path.c_str(), &handle);
        if (size == 0)
        {
            return L"";
        }

        std::vector<unsigned char> data(size);
        if (!GetFileVersionInfoW(path.c_str(), handle, size, data.data()))
        {
            return L"";
        }

        VS_FIXEDFILEINFO* info = nullptr;
        UINT infoLength = 0;
        if (!VerQueryValueW(data.data(), L"\\", reinterpret_cast<void**>(&info), &infoLength) || info == nullptr)
        {
            return L"";
        }

        wchar_t version[128] = {};
        swprintf_s(
            version,
            L"%u.%u.%u.%u",
            HIWORD(info->dwProductVersionMS),
            LOWORD(info->dwProductVersionMS),
            HIWORD(info->dwProductVersionLS),
            LOWORD(info->dwProductVersionLS));
        return version;
    }

    std::wstring QueryArchitecture(HANDLE processHandle)
    {
        BOOL isWow64 = FALSE;
        if (IsWow64Process(processHandle, &isWow64) && isWow64)
        {
            return L"x86";
        }

        return L"x64";
    }

    uintptr_t QueryModuleBaseAddress(DWORD processId)
    {
        HANDLE snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPMODULE | TH32CS_SNAPMODULE32, processId);
        if (snapshot == INVALID_HANDLE_VALUE)
        {
            return 0;
        }

        MODULEENTRY32W module = {};
        module.dwSize = sizeof(module);
        uintptr_t baseAddress = 0;

        if (Module32FirstW(snapshot, &module))
        {
            baseAddress = reinterpret_cast<uintptr_t>(module.modBaseAddr);
        }

        CloseHandle(snapshot);
        return baseAddress;
    }

    bool FindFmProcess(DWORD& processId)
    {
        HANDLE snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
        if (snapshot == INVALID_HANDLE_VALUE)
        {
            return false;
        }

        PROCESSENTRY32W entry = {};
        entry.dwSize = sizeof(entry);

        bool found = false;
        if (Process32FirstW(snapshot, &entry))
        {
            do
            {
                if (_wcsicmp(entry.szExeFile, L"fm.exe") == 0)
                {
                    processId = entry.th32ProcessID;
                    found = true;
                    break;
                }
            }
            while (Process32NextW(snapshot, &entry));
        }

        CloseHandle(snapshot);
        return found;
    }

    int PopulateProcessInfo(DWORD processId, StatlynProcessInfo* processInfo)
    {
        if (processInfo == nullptr)
        {
            return STATLYN_STATUS_INVALID_ARGUMENT;
        }

        *processInfo = {};
        processInfo->processId = static_cast<uint32_t>(processId);
        processInfo->detected = 1;

        HANDLE handle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION | PROCESS_VM_READ, FALSE, processId);
        if (handle == nullptr)
        {
            SetLastErrorText("FM26 was detected, but Windows denied read/query access.");
            processInfo->readOnlyAccess = 0;
            return GetLastError() == ERROR_ACCESS_DENIED ? STATLYN_STATUS_ACCESS_DENIED : STATLYN_STATUS_INTERNAL_ERROR;
        }

        processInfo->readOnlyAccess = 1;
        const std::wstring path = QueryProcessPath(handle);
        CopyWide(processInfo->executablePath, 520, path);
        CopyWide(processInfo->productVersion, 128, QueryProductVersion(path));
        CopyWide(processInfo->architecture, 32, QueryArchitecture(handle));
        processInfo->moduleBaseAddress = QueryModuleBaseAddress(processId);
        CloseHandle(handle);
        g_lastProcessInfo = *processInfo;
        SetLastErrorText("");
        return STATLYN_STATUS_OK;
    }
}

extern "C"
{
    STATLYN_API int Statlyn_DetectFM26(StatlynProcessInfo* processInfo)
    {
        DWORD processId = 0;
        if (!FindFmProcess(processId))
        {
            if (processInfo != nullptr)
            {
                *processInfo = {};
            }

            SetLastErrorText("Football Manager 26 process fm.exe was not found.");
            return STATLYN_STATUS_NOT_FOUND;
        }

        return PopulateProcessInfo(processId, processInfo);
    }

    STATLYN_API int Statlyn_GetProcessInfo(StatlynProcessInfo* processInfo)
    {
        if (processInfo == nullptr)
        {
            return STATLYN_STATUS_INVALID_ARGUMENT;
        }

        *processInfo = g_lastProcessInfo;
        return g_lastProcessInfo.detected ? STATLYN_STATUS_OK : Statlyn_DetectFM26(processInfo);
    }

    STATLYN_API int Statlyn_OpenReadOnly(uint32_t processId)
    {
        Statlyn_Close();
        HANDLE handle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION | PROCESS_VM_READ, FALSE, processId);
        if (handle == nullptr)
        {
            SetLastErrorText("Unable to open FM26 with read-only process permissions.");
            return GetLastError() == ERROR_ACCESS_DENIED ? STATLYN_STATUS_ACCESS_DENIED : STATLYN_STATUS_INTERNAL_ERROR;
        }

        g_processHandle = handle;
        g_processId = processId;
        SetLastErrorText("");
        return STATLYN_STATUS_OK;
    }

    STATLYN_API void Statlyn_Close()
    {
        if (g_processHandle != nullptr)
        {
            CloseHandle(g_processHandle);
            g_processHandle = nullptr;
            g_processId = 0;
        }
    }

    STATLYN_API int Statlyn_ValidateBuild()
    {
        SetLastErrorText("FM26 detected, but no validated memory map is registered for this build yet.");
        return STATLYN_STATUS_UNSUPPORTED_BUILD;
    }

    STATLYN_API int Statlyn_ReadSnapshot(char* buffer, int bufferLength)
    {
        const std::string payload =
            "{\"status\":\"unsupported_build\",\"players\":[],\"message\":\"No validated FM26 memory map is active. Statlyn does not return fixture players.\"}";
        CopyUtf8(buffer, bufferLength, payload);
        SetLastErrorText("Snapshot unavailable because this FM26 build has no validated memory map.");
        return STATLYN_STATUS_UNSUPPORTED_BUILD;
    }

    STATLYN_API int Statlyn_GetLastError(char* buffer, int bufferLength)
    {
        return CopyUtf8(buffer, bufferLength, g_lastError);
    }

    STATLYN_API int Statlyn_GetDiagnostics(char* buffer, int bufferLength)
    {
        char diagnostics[1024] = {};
        snprintf(
            diagnostics,
            sizeof(diagnostics),
            "{\"connector\":\"Statlyn.NativeConnector\",\"processId\":%lu,\"handleOpen\":%s,\"buildSupport\":\"unsupported\"}",
            static_cast<unsigned long>(g_processId),
            g_processHandle == nullptr ? "false" : "true");
        return CopyUtf8(buffer, bufferLength, diagnostics);
    }

    STATLYN_API int Statlyn_GetConnectorVersion(char* buffer, int bufferLength)
    {
        return CopyUtf8(buffer, bufferLength, "0.1.0");
    }
}
