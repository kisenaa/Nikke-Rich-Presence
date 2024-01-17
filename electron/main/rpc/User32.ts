
import {Library} from '@breush/ffi-napi';

import * as M from './DModel'
import { AsyncFnModel, ExpandFnModel } from './DModel';
import * as W from "./WModel"

interface Win32Fns extends AsyncFnModel {
    FindWindowExW: {
       (
        hwndParent: M.HWND,
        hwndChildAfter: M.HWND,
        lpszClass: M.LPCTSTR | null,
        lpszWindow: M.LPCTSTR | null,
      ): M.HWND;
      async (
        hwndParent :M.HWND,
        hwndChildAfter: M.HWND,
        lpszClass: M.LPCTSTR | null,
        lpszWindow: M.LPCTSTR | null,
        cb: (err: Error | null, result: M.HWND | M.INT) => void,
      ): Promise<M.HWND>;
    },
    GetWindowThreadProcessId: {
        (hWnd: M.HWND, lpdwProcessId: M.LPDWORD): M.DWORD;
        async (hWnd: M.HWND, lpdwProcessId: M.LPDWORD): Promise<M.DWORD>;
    }
  };

export const user32: ExpandFnModel<Win32Fns> = Library('user32.dll', {
    FindWindowExW: [W.HWND, [W.HWND, W.HWND, W.LPCTSTR, W.LPCTSTR]],
    GetWindowThreadProcessId:[W.DWORD, [W.HWND, W.LPDWORD]]
}) as ExpandFnModel<Win32Fns>; 