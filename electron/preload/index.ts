/* eslint-disable @typescript-eslint/no-explicit-any */
import { contextBridge, ipcRenderer } from "electron"

const domReady = (condition: DocumentReadyState[] = ['complete', 'interactive']) => {
  return new Promise(resolve => {
    if (condition.includes(document.readyState)) {
      resolve(true)
    } else {
      document.addEventListener('readystatechange', () => {
        if (condition.includes(document.readyState)) {
          resolve(true)
        }
      })
    }
  })
}

const safeDOM = {
  append(parent: HTMLElement, child: HTMLElement) {
    if (!Array.from(parent.children).find(e => e === child)) {
      return parent.appendChild(child)
    }
  },
  remove(parent: HTMLElement, child: HTMLElement) {
    if (Array.from(parent.children).find(e => e === child)) {
      return parent.removeChild(child)
    }
  },
}

/**
 * https://tobiasahlin.com/spinkit
 * https://connoratherton.com/loaders
 * https://projects.lukehaas.me/css-loaders
 * https://matejkustec.github.io/SpinThatShit
 */
const useLoading = () => {
  const className = `loaders-css__square-spin`
  const styleContent = `
c dewszqaqswderftyfrdcsza
.${className} > div {
  animation-fill-mode: both;
  width: 50px;
  height: 50px;
  background: red;
  animation: square-spin 3s 0s cubic-bezier(0.09, 0.57, 0.49, 0.9) infinite;
}
.app-loading-wrap {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9;
}
    `
  const oStyle = document.createElement('style')
  const oDiv = document.createElement('div')

  oStyle.id = 'app-loading-style'
  oStyle.innerHTML = styleContent
  oDiv.className = 'app-loading-wrap'
  oDiv.innerHTML = `<div class="${className}"><div></div></div>`

  return {
    appendLoading() {
      safeDOM.append(document.head, oStyle)
      safeDOM.append(document.body, oDiv)
    },
    removeLoading() {
      safeDOM.remove(document.head, oStyle)
      safeDOM.remove(document.body, oDiv)
    },
  }
}

// ----------------------------------------------------------------------

const { appendLoading, removeLoading } = useLoading()
domReady().then(appendLoading)

window.onmessage = (ev) => {
  ev.data.payload === 'removeLoading' && removeLoading()
}

//setTimeout(removeLoading, 4999)

contextBridge.exposeInMainWorld('Bridge', {
  /* 
  Main -> Renderer
  */
  onMessageFromMain: (callback: any) => ipcRenderer.on('messages', callback),
  setDiscord: (callback: any) => ipcRenderer.on('setDiscord', callback),
  setIsDiscordOn: (callback: any) => ipcRenderer.on('setIsDiscordOn', callback),
  setIsNikkeFound: (callback: any) => ipcRenderer.on('setIsNikkeFound', callback),
  /*
  Renderer -> Main
  */
  messageResponse: (value: string) => ipcRenderer.send('response-back', value),
  setRichPresence: (status: boolean) => ipcRenderer.send('setRichPresence', status),
  store: {
    get(key:string) {
      return ipcRenderer.sendSync('electron-store-get', key);
    },
    set(property:string, val:any) {
      ipcRenderer.send('electron-store-set', property, val);
    },
  },
})