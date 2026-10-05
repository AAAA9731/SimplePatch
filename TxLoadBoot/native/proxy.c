// version.dll proxy: forwards the real version.dll and, from a worker thread, boots TxLoad\TxLoadBoot.dll
// inside the game's Mono runtime. No inline hooks and no doorstop files, so it can sit next to BepInEx.
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <string.h>

#define FWD(n) __pragma(comment(linker, "/export:" #n "=C:\\Windows\\System32\\version." #n))
FWD(GetFileVersionInfoA) FWD(GetFileVersionInfoByHandle) FWD(GetFileVersionInfoExA) FWD(GetFileVersionInfoExW)
FWD(GetFileVersionInfoSizeA) FWD(GetFileVersionInfoSizeExA) FWD(GetFileVersionInfoSizeExW) FWD(GetFileVersionInfoSizeW)
FWD(GetFileVersionInfoW) FWD(VerFindFileA) FWD(VerFindFileW) FWD(VerInstallFileA) FWD(VerInstallFileW)
FWD(VerLanguageNameA) FWD(VerLanguageNameW) FWD(VerQueryValueA) FWD(VerQueryValueW)

typedef void* (*fn_get_root_domain)(void);
typedef void* (*fn_thread_attach)(void*);
typedef void (*fn_thread_detach)(void*);
typedef void* (*fn_thread_current)(void);
typedef void* (*fn_assembly_name_new)(const char*);
typedef void* (*fn_assembly_loaded)(void*);
typedef void* (*fn_assembly_open)(const char*, int*);
typedef void* (*fn_assembly_get_image)(void*);
typedef void* (*fn_class_from_name)(void*, const char*, const char*);
typedef void* (*fn_class_get_method)(void*, const char*, int);
typedef void* (*fn_runtime_invoke)(void*, void*, void**, void**);

static void Log(const wchar_t* dir, const char* msg)
{
	wchar_t p[MAX_PATH * 2];
	wsprintfW(p, L"%s\\TxLoad\\native.log", dir);
	HANDLE h = CreateFileW(p, FILE_APPEND_DATA, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_ALWAYS, 0, NULL);
	if (h != INVALID_HANDLE_VALUE) {
		DWORD w; WriteFile(h, msg, (DWORD)lstrlenA(msg), &w, NULL); WriteFile(h, "\r\n", 2, &w, NULL); CloseHandle(h);
	}
}

// BepInEx (doorstop) runs its preloader on the game's main thread right after the Mono domain appears.
// Attaching/detaching a thread to Mono during that window breaks the preloader's DynDll imports, so when
// BepInEx is installed we stay completely away from Mono until its chainloader reports it has finished.
static int FileExists(const wchar_t* dir, const wchar_t* rel)
{
	wchar_t p[MAX_PATH * 2];
	wsprintfW(p, L"%s\\%s", dir, rel);
	return GetFileAttributesW(p) != INVALID_FILE_ATTRIBUTES;
}

static void WaitForBepInEx(const wchar_t* dir)
{
	wchar_t p[MAX_PATH * 2];
	char buf[4096];
	int i;
	if (!FileExists(dir, L"BepInEx\\core\\BepInEx.Unity.Mono.Preloader.dll") || !FileExists(dir, L"winhttp.dll")) return;
	wsprintfW(p, L"%s\\BepInEx\\LogOutput.log", dir);
	Log(dir, "BepInEx detected; waiting for its startup to finish");
	for (i = 0; i < 2400; i++) { // up to ~120 s; also covers BepInEx's disk log being disabled
		HANDLE h = CreateFileW(p, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, NULL, OPEN_EXISTING, 0, NULL);
		if (h != INVALID_HANDLE_VALUE) {
			LARGE_INTEGER size; DWORD n = 0;
			if (GetFileSizeEx(h, &size)) {
				LONGLONG from = size.QuadPart > (LONGLONG)sizeof(buf) - 1 ? size.QuadPart - ((LONGLONG)sizeof(buf) - 1) : 0;
				LARGE_INTEGER pos; pos.QuadPart = from;
				if (SetFilePointerEx(h, pos, NULL, FILE_BEGIN) && ReadFile(h, buf, sizeof(buf) - 1, &n, NULL)) {
					buf[n] = 0;
					if (strstr(buf, "Chainloader startup complete")) { CloseHandle(h); Sleep(1000); return; }
				}
			}
			CloseHandle(h);
		}
		Sleep(50);
	}
	Log(dir, "BepInEx wait timed out");
}

static DWORD WINAPI Worker(LPVOID unused)
{
	wchar_t dir[MAX_PATH * 2], dll[MAX_PATH * 2];
	char dll8[MAX_PATH * 4];
	int i;
	(void)unused;
	GetModuleFileNameW(NULL, dir, MAX_PATH * 2);
	for (i = lstrlenW(dir) - 1; i > 0 && dir[i] != L'\\'; i--) {}
	dir[i] = 0;
	wsprintfW(dll, L"%s\\TxLoad\\TxLoadBoot.dll", dir);
	if (GetFileAttributesW(dll) == INVALID_FILE_ATTRIBUTES) return 0;
	if (GetEnvironmentVariableW(L"TXLOAD_DISABLE", NULL, 0) > 0) return 0;
	WideCharToMultiByte(CP_UTF8, 0, dll, -1, dll8, sizeof(dll8), NULL, NULL);
	WaitForBepInEx(dir);

	HMODULE mono = NULL; int seen = 0;
	fn_get_root_domain get_root; fn_assembly_name_new name_new; fn_assembly_loaded loaded;
	fn_thread_attach attach0; fn_thread_detach detach0; fn_thread_current cur0;
	for (i = 0; i < 6000; i++) { // up to ~120 s
		mono = GetModuleHandleW(L"mono-2.0-bdwgc.dll");
		if (mono) {
			if (!seen) { seen = 1; Log(dir, "mono module found"); Sleep(3000); }
			get_root = (fn_get_root_domain)GetProcAddress(mono, "mono_get_root_domain");
			name_new = (fn_assembly_name_new)GetProcAddress(mono, "mono_assembly_name_new");
			loaded = (fn_assembly_loaded)GetProcAddress(mono, "mono_assembly_loaded");
			attach0 = (fn_thread_attach)GetProcAddress(mono, "mono_thread_attach");
			detach0 = (fn_thread_detach)GetProcAddress(mono, "mono_thread_detach");
			cur0 = (fn_thread_current)GetProcAddress(mono, "mono_thread_current");
			if (get_root && name_new && loaded && attach0 && detach0 && cur0 && get_root()) {
				// mono_assembly_loaded needs a current domain, so attach for the check only (a parked attached thread would block GC).
				int ok;
				attach0(get_root());
				void* an = name_new("Assembly-CSharp");
				ok = (an && loaded(an)) ? 1 : 0;
				detach0(cur0());
				if (ok) break;
			}
		}
		Sleep(50);
	}
	if (i >= 6000) { Log(dir, "timeout waiting for mono/Assembly-CSharp"); return 0; }

	fn_thread_attach attach = (fn_thread_attach)GetProcAddress(mono, "mono_thread_attach");
	fn_thread_detach detach = (fn_thread_detach)GetProcAddress(mono, "mono_thread_detach");
	fn_thread_current cur = (fn_thread_current)GetProcAddress(mono, "mono_thread_current");
	fn_assembly_open open_ = (fn_assembly_open)GetProcAddress(mono, "mono_assembly_open");
	fn_assembly_get_image get_image = (fn_assembly_get_image)GetProcAddress(mono, "mono_assembly_get_image");
	fn_class_from_name cls = (fn_class_from_name)GetProcAddress(mono, "mono_class_from_name");
	fn_class_get_method getm = (fn_class_get_method)GetProcAddress(mono, "mono_class_get_method_from_name");
	fn_runtime_invoke invoke = (fn_runtime_invoke)GetProcAddress(mono, "mono_runtime_invoke");
	if (!attach || !open_ || !get_image || !cls || !getm || !invoke) { Log(dir, "mono exports missing"); return 0; }

	attach(get_root());
	int status = 0;
	void* asm_ = open_(dll8, &status);
	if (!asm_) { Log(dir, "mono_assembly_open failed"); goto done; }
	void* klass = cls(get_image(asm_), "TxLoadBoot", "Entry");
	void* method = klass ? getm(klass, "Start", 0) : NULL;
	if (!method) { Log(dir, "TxLoadBoot.Entry.Start not found"); goto done; }
	void* exc = NULL;
	invoke(method, NULL, NULL, &exc);
	Log(dir, exc ? "Entry.Start threw" : "Entry.Start ok");
done:
	if (detach && cur) detach(cur());
	return 0;
}

BOOL WINAPI DllMain(HINSTANCE h, DWORD reason, LPVOID r)
{
	(void)r;
	if (reason == DLL_PROCESS_ATTACH) {
		DisableThreadLibraryCalls(h);
		HANDLE t = CreateThread(NULL, 0, Worker, NULL, 0, NULL);
		if (t) CloseHandle(t);
	}
	return TRUE;
}
