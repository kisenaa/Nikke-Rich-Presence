import { useEffect, useState } from 'react'
import {FaGithub} from 'react-icons/fa'

import discordImg from './assets/discordblue.png'
import RpcImage from './components/RpcImage'
import { DiscordInfo } from './types/Discord'

function App() {

  const [profileLink, setProfileLink] = useState<null|string>(null)
  const [username, setUsername] = useState('Unknown')
  const [isDiscordOn, setIsDiscordOn] = useState(false)
  const [isNikkeFound, setIsNikkeFound] = useState(false)
  const [isRpcEnabled, setIsRpcEnabled] = useState(true)

  const [details, setDetails] = useState<null|string>(null)
  const [states, setStates] = useState<null|string>(null)

  const [imageKey, setImageKey] = useState("nikke-default")

  const handleSetRpc = (value: boolean) => {
    setIsRpcEnabled(value)
    window.Bridge.setRichPresence(value)
  }


  useEffect(() => {
    setImageKey(window.Bridge.store.get('largeImageKey'))
    setDetails(window.Bridge.store.get('details'))
    setStates(window.Bridge.store.get('states'))


    window.Bridge.setDiscord((_event, value) => {
      const discordInfo = value as DiscordInfo
      setProfileLink(discordInfo.avatarUrl)
      setUsername(discordInfo.username)
    })

    window.Bridge.setIsDiscordOn((_event, value) => {
      setIsDiscordOn(value as boolean)
    })

    window.Bridge.setIsNikkeFound((_event, value) => {
      setIsNikkeFound(value as boolean)
    })

    return() => {
      window.Bridge.removeSetDiscord()
      window.Bridge.removeIsDiscordOn()
      window.Bridge.removeIsNikkeFound()
    }
  },[])


  return (
    <div className='content-center items-center px-3'>
      <div className='mt-5 flex justify-between pr-[10%]'>
        <div className='flex flex-col gap-1'>
          <div className='flex w-[370px] flex-col pl-1 '>
              Image Selector
              <select className='my-1 h-8 max-w-96 pl-1' value={imageKey} onChange={async (e) => {window.Bridge.store.set('largeImageKey', e.target.value);setImageKey(e.target.value);}}>
                <option value='nikke-default'>Default Image</option>
                <option value='blancc'>Blanc</option>
                <option value='scarlet2'>Scarlet</option>
                <option value='scarlet_bshadow'>Scarlet BlackShadow</option>

                <option value='privaty'>Privaty</option>
                <option value='viper'>Viper</option>
              </select >
          </div>

          <div className='flex flex-col pl-1'>
          Details
            <input className='my-1 h-[25px] max-w-96 pl-1' placeholder='Enter Details (optional)' value={details || ''} maxLength={32} onChange={async (e) => {window.Bridge.store.set('details', e.target.value);setDetails(e.target.value)}}></input>
          </div>

          <div className='flex flex-col pl-1'>
          State
            <input className='my-1 h-[25px] max-w-96' placeholder='Enter State (optional)' value={states || ''} maxLength={32} onChange={async (e) => {window.Bridge.store.set('states', e.target.value);setStates(e.target.value)}}></input>
          </div>

          <div className='flex flex-row justify-center gap-6 pt-4'>
            <button className={isRpcEnabled ? "cursor-not-allowed text-gray-400 disabled:opacity-75" : ""} disabled={isRpcEnabled} onClick={() => handleSetRpc(true)}>
              Enable
            </button>
            <button className={!isRpcEnabled ? "cursor-not-allowed text-gray-400 disabled:opacity-75" : ""} disabled={!isRpcEnabled} onClick={() => handleSetRpc(false)}>
              Disable
            </button>
          </div>
        </div>

        <div className='items-center pt-3'>
          <div className='flex gap-3'>
          <img className="rounded-full" src={(profileLink != null) ? profileLink : discordImg} alt="profile-pic" width={75} />
            <span className='flex items-center justify-center text-lg font-semibold'>{username}</span>
          </div>

          <div className='mb-1 mt-4'>
            <span className='mb-1 mr-1 mt-4'>Discord Client : </span>

            {isDiscordOn ? (<span className='font-semibold text-green-600'>Found</span>
            ) : (
            <span className='font-semibold text-red-600'>Not Found</span>
            )}
          </div>


          <span className='mr-1'>Nikke Game &nbsp;&nbsp; : </span>
          <span className={`font-semibold ${isNikkeFound ? "text-green-600" : "text-red-600"}`}>
            {isNikkeFound && isDiscordOn ? "Found" : "Unknown"}
          </span>

          <div className='items-center pt-4 text-center font-semibold'>
            Rich Presence is now 
            {isRpcEnabled ? " enabled !" : " Disabled !"}
          </div>
        </div>
      </div>

      <div className="relative flex w-full items-center pb-1 pt-4">
        <div className="grow border-t border-solid border-gray-400"></div>
        <span className="mx-4 shrink font-semibold text-gray-400">Visualization</span>
        <div className="grow border-t border-solid  border-gray-400"></div>
      </div>


      <div className="w-fit px-3">
          <span className='text-2xl font-bold'> Playing a game </span>

        <div className="mt-4 flex flex-row items-center">
          <RpcImage path={imageKey}/>

          <div className='pl-4'>
            <div>Goddess of Victory: Nikke</div>
            <div>{details}</div>
            <div>{states}</div>
            <div>00:01 elapsed</div>
          </div>
        </div>
      </div>

      <div className="absolute bottom-0 left-1/2 flex -translate-x-1/2 items-center space-x-2 text-center">
        <a href="https://github.com/kisenaa/Nikke-Rich-Presence" target="_blank" rel="noopener noreferrer" className="flex items-center space-x-2 text-gray-500 no-underline">
          <FaGithub />
          <span className='font-semibold'>github.com/kisenaa/Nikke-Rich-Presence</span>
        </a>
    </div>
  </div>
  )
}

export default App