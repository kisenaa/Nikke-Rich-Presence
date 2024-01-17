// @breush__ffi-napi.d.ts

// Import the types from @types/ffi-napi

// Extend the types for @breush/ffi-napi
declare module '@breush/ffi-napi' {
  import { Library as Librarys} from "ffi-napi";
  /*
  Provides a friendly API on-top of DynamicLibrary and ForeignFunction.
  */
  export const Library = Librarys
}

