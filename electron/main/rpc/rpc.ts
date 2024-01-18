import { Client } from "@xhayper/discord-rpc";

import { user32 } from "./User32";
import { gc,sendDiscordInfo, setDiscordOn, setIsNikkeFound } from "..";

// Discord RPC Client
export const client = new Client({
    clientId: "1196659687970586676"
});


const lpszClass = Buffer.from("UnityWndClass\0", 'ucs2')
const lpszWindow = Buffer.from("NIKKE\0", 'ucs2')
const lpdwProcessId = Buffer.allocUnsafe(8)

let isEnabled = true;
let isPlaying = false;
let isInitialized = false;
let pid = 1

let largeImageKey = "nikke-default"
let details: string|null = null
let states: string|null = null


export const setLargeImageKey = (value: string|null) => {
    if(value != null && value != undefined) largeImageKey = value
}

export const setDetails = (value: string|null) => {
    details = value
    if (details != null && details != '' && details != ' ' ) {
        if (details.length < 2) {
          details += '\u2800\u2800'; // Add U+2800 to the details string
        }
      }
      
}

export const setStates = (value: string|null) => {
    states = value
    if (states != null && states != '' && states != ' ') {
        if (states.length < 2) states += '\u2800\u2800'
    }
}

// Attempt first login
export const AttemptLogin = async() => {
    try {
        await client.login()
    }
    catch {/* empty */}
}

// Delay
const delay = (ms: number): Promise<void> => {
    return new Promise(resolve => setTimeout(resolve, ms));
  };

export const DisableRichPresence = async () => {
    isPlaying = false
    isEnabled = false;
    isInitialized = false;
    await delay(2000)
    try {
        await client.user?.clearActivity(pid)
    }
    catch (Error) { /* empty */}
    await delay(200)
    try {
        await client.user?.clearActivity(pid)
        await client.user?.clearActivity()
    }
    catch (Error) { /* empty */}
    client.removeAllListeners()
    // Sometimes clear activity didnt work, so make sure to call it a few times
}

const TryLogin = async () => {
    let isLogin = false;

    if (client.isConnected) {
        isLogin = true
        sendDiscordInfo()
        setDiscordOn(true)
    } else {
        setDiscordOn(false)
    }

    while (!isLogin) {
        if(!isEnabled) {
            isLogin = true
            break
        }
        await delay(1000)
        client.removeAllListeners()

        if (client.isConnected) {
            sendDiscordInfo()
            setDiscordOn(true)
            isLogin = true
            break
        }

        try {
            await client.login()
        } 
        catch(error) {
            continue
        }
        gc();
    }
}


export const EnableRichPresence = async () => {
    if (!isEnabled) isEnabled = true
    if (isInitialized) return
    isInitialized = true

    await TryLogin()
    
    while (isEnabled) {
        await delay(1000)
        client.removeAllListeners()


        if (!client.isConnected) {
            isPlaying = false
            setDiscordOn(false)
            await TryLogin()
            if (!isEnabled) break
        }
       
        const handle = user32.FindWindowExW(0,0,lpszClass,lpszWindow)

        if (handle == 0) {
            if (isPlaying) {
                setIsNikkeFound(false)
                isPlaying = false;
                try {
                    await client.user?.clearActivity(pid)
                    client.user?.clearActivity()
                }
                catch(error) { /* empty */ }
            }
            continue
        }

        const isSuccess = user32.GetWindowThreadProcessId(handle, lpdwProcessId)
        if (isSuccess == 0) {
            continue
        }
        if (!isPlaying) {
            isPlaying = true
            setIsNikkeFound(true)
            pid = lpdwProcessId.readUInt16LE(0)
            try {
                if (!isEnabled) break
                await client.user?.setActivity({
                    startTimestamp: Date.now(),
                    state : states || undefined,
                    details : details || undefined,
                    largeImageKey: largeImageKey,
                    largeImageText: "Goddess of Victory: Nikke"
                }, pid)
            }
            catch(error) {
                console.log(error)
                isPlaying = false
                if (client.user?.presence != undefined) {
                    try {
                        await client.user?.clearActivity(pid);
                    }
                    catch (Error) { /* empty */}
                }
            }
        }
        gc();
    }
    isInitialized = false
    isPlaying = false
}




