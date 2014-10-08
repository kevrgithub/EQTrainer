// REFERENCES and RESOURCES
// =======================
// Auto Unload DLL: http://www.unknowncheats.me/forum/613953-post43.html
// EQ Mac Functions: https://github.com/sodcheats/eqmac/blob/master/eqmac/include/eqmac.hpp

#include "windows.h"
#include "Stdafx.h"

void OnAttach( HMODULE hModule ) {
	typedef void (__thiscall* CGCamera__ResetView)();
	CGCamera__ResetView ResetView = (CGCamera__ResetView)0x004B459C;
	ResetView();

	FreeLibraryAndExitThread( hModule, 0 );                               
	ExitThread( 0 );
}

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
					 )
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		CreateThread( NULL, 0, (LPTHREAD_START_ROUTINE)OnAttach, hModule, 0, NULL );            
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}