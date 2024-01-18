import { IpcRendererEvent } from "electron/renderer";

// bridge.d.ts
type ICallback = (_event: IpcRendererEvent, value: unknown) => void;

interface ElectronStore {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  get(key: string): any;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
  set(property: string, val: any): void;
}

interface BridgeAPI {
  /* 
  Main -> Renderer
  */
  onMessageFromMain(callback: ICallback): void;
  setDiscord(callback: ICallback): void;
  setIsDiscordOn(callback: ICallback): void;
  setIsNikkeFound(callback: ICallback): void;
  removeSetDiscord(): void;
  removeIsDiscordOn(): void;
  removeIsNikkeFound(): void;

  /*
  Renderer -> Main
  */
  messageResponse(value: string): void;
  setRichPresence(status: boolean): void;
  store: ElectronStore;
}

declare global {
  interface Window {
    Bridge: BridgeAPI;
  }
}

export{}