import blancc from "../assets/blancc.jpg"
import defaultImage from "../assets/nikke-default.png"
import privaty from "../assets/privaty.png"
import scarlet2 from "../assets/scarlet.jpg"
import scarlet_bshadow from "../assets/scarlet_bs.png"
import viper from "../assets/viper.png"

const PicturePath = {
    defaultImage,
    blancc,
    scarlet2,
    scarlet_bshadow,
    privaty,
    viper,
} as const;
 
type PicturePathKey = keyof typeof PicturePath;

const RpcImage = ({ path }: { path: string }) => {
    const picturePath = PicturePath[path as PicturePathKey] || PicturePath.defaultImage;
    return (
        <img src={picturePath} alt="Goddess Of Victory: Nikke" className="w-[110px]" />
      );
};

export default RpcImage