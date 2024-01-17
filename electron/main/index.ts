import { app, BrowserWindow, ipcMain,Menu, shell, Tray} from 'electron'
import Store from 'electron-store';
import { release } from 'node:os'
import { join } from 'node:path'
import { setFlagsFromString } from 'v8';
import { runInNewContext } from 'vm';

import { AttemptLogin, client, DisableRichPresence, EnableRichPresence, setDetails, setLargeImageKey, setStates } from './rpc/rpc'
import { update } from './update'
// The built directory structure
//
// ├─┬ dist-electron
// │ ├─┬ main
// │ │ └── index.js    > Electron-Main
// │ └─┬ preload
// │   └── index.js    > Preload-Scripts
// ├─┬ dist
// │ └── index.html    > Electron-Renderer

setFlagsFromString('--expose_gc');
export const gc = runInNewContext('gc'); // nocommit

//
process.traceProcessWarnings = true
process.env.DIST_ELECTRON = join(__dirname, '../')
process.env.DIST = join(process.env.DIST_ELECTRON, '../dist')
process.env.VITE_PUBLIC = process.env.VITE_DEV_SERVER_URL
  ? join(process.env.DIST_ELECTRON, '../public')
  : process.env.DIST

// Disable GPU Acceleration for Windows 7
if (release().startsWith('6.1')) app.disableHardwareAcceleration()

// Set application name for Windows 10+ notifications
if (process.platform === 'win32') app.setAppUserModelId(app.getName())

if (!app.requestSingleInstanceLock()) {
  app.quit()
  process.exit(0)
}

// Remove electron security warnings
// This warning only shows in development mode
// Read more on https://www.electronjs.org/docs/latest/tutorial/security
// process.env['ELECTRON_DISABLE_SECURITY_WARNINGS'] = 'true'

let win: BrowserWindow | null = null
// Here, you can also use other preload
const preload = join(__dirname, '../preload/index.js')
const url = process.env.VITE_DEV_SERVER_URL
const indexHtml = join(process.env.DIST, 'index.html')

const store = new Store();


const createWindow = async() => {
  win = new BrowserWindow({
    width: 800,
    height: 550,
    resizable: false,
    roundedCorners: true,
    title: 'Main window',
    icon: join(process.env.VITE_PUBLIC, 'apps.png'),
    webPreferences: {
      preload,
      // Warning: Enable nodeIntegration and disable contextIsolation is not secure in production
      // Consider using contextBridge.exposeInMainWorld
      // Read more on https://www.electronjs.org/docs/latest/tutorial/context-isolation
      nodeIntegration: true,
      contextIsolation: true,
    },
  })

  if (url) { // electron-vite-vue#298
    win.loadURL(url)
    // Open devTool if the app is not packaged
    win.webContents.openDevTools()
  } else {
    win.loadFile(indexHtml)
  }

  // Disable menu to improve performance
  win.setMenu(null)

  // Tray
  const trayIcon = new Tray(join(process.env.VITE_PUBLIC, 'apps.png'))
  trayIcon.setToolTip('Nikke Rich Presence')

  const contextMenu = Menu.buildFromTemplate([
    { label: 'Open App', click: () => {
        win?.show();
    } },
    { label: 'Quit', click: () => {
        win?.destroy();
        app.quit();
    } }
  ]);

  trayIcon.setContextMenu(contextMenu)

  // Make all links open with the browser, not with the application
  win.webContents.setWindowOpenHandler(({ url }) => {
    if (url.startsWith('https:')) shell.openExternal(url)
    return { action: 'deny' }
  })

  win.webContents.on('did-finish-load', async () => {
    win?.webContents.send('messages', 'Hello Word From Mains')
    await AttemptLogin()
    EnableRichPresence()
  })

  win.on('close', (event) => {
    event.preventDefault()
    win?.hide()
  })

  win.on('minimize', () => {
    win?.hide()
  })
  // Apply electron-updater
  update(win)
}

const InitializeDiscordProperties = () => {
  const largeImageKey = store.get('largeImageKey')
  if (largeImageKey == null) {
    store.set('largeImageKey', 'nikke-default')
  } else {
    setLargeImageKey(largeImageKey as string|null)
  }

  setDetails(store.get('details') as string|null)

  setStates(store.get('states') as string|null)
}

app.whenReady().then(async () => {
  // Init the value of discord rpc properties from electron-store
  InitializeDiscordProperties()

  await createWindow()

  ipcMain.on('response-back', async(_event, value) => {
    console.log("got a response: ", value)
  })

  ipcMain.on('setRichPresence', async(_event, status) => {
    status == true ? EnableRichPresence() : DisableRichPresence()
  })

  ipcMain.on('electron-store-get', async (event, val) => {
    event.returnValue = store.get(val);
  });
  
  ipcMain.on('electron-store-set', async (event, key, val) => {
    if (key == 'largeImageKey') setLargeImageKey(val)
    else if (key == 'details') setDetails(val)
    else if (key == 'states') setStates(val)
    store.set(key, val);
  });
})

app.on('window-all-closed', () => {
  win = null
  if (process.platform !== 'darwin') app.quit()
})

app.on('second-instance', () => {
  if (win) {
    // Focus on the main window if the user tried to open another
    if (win.isMinimized()) win.restore()
    win.focus()
  }
})

app.on('activate', () => {
  const allWindows = BrowserWindow.getAllWindows()
  if (allWindows.length) {
    allWindows[0].focus()
  } else {
    createWindow()
  }
})

export const sendDiscordInfo = () => {
  const DiscordInfo = {'username': client.user?.username, 'avatarUrl': client.user?.avatarUrl}
  win?.webContents.send('setDiscord', DiscordInfo)
}

export const setDiscordOn = (isOn: boolean) => {
  win?.webContents.send('setIsDiscordOn', isOn)
}

export const setIsNikkeFound = (isFound: boolean) => {
  win?.webContents.send('setIsNikkeFound', isFound)
}
