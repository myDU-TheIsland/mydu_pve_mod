export const ChevronUpIcon = ({size}) => {
    return (<svg className="w-6 h-6 text-gray-800 dark:text-white" aria-hidden="true" xmlns="http://www.w3.org/2000/svg"
                 width={size} height={size} fill="none" viewBox="0 0 24 24">
        <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M16 15L12 11L8 15m8-4L12 7L8 11"/>
    </svg>);
}

export const DotIcon = ({size, color = "currentColor"}) => {
    return (<svg className="text-gray-800 dark:text-white" aria-hidden="true" xmlns="http://www.w3.org/2000/svg"
                 width={size} height={size} fill={color} viewBox="0 0 10 10">
        <circle cx="5" cy="5" r="3"/>
    </svg>);
}

export const TargetIcon2 = ({fill}) => {
    return <svg style={{width: 24, height: 24, fill}} viewBox="0 0 20 20">
        <path
            d="M17.659,9.597h-1.224c-0.199-3.235-2.797-5.833-6.032-6.033V2.341c0-0.222-0.182-0.403-0.403-0.403S9.597,2.119,9.597,2.341v1.223c-3.235,0.2-5.833,2.798-6.033,6.033H2.341c-0.222,0-0.403,0.182-0.403,0.403s0.182,0.403,0.403,0.403h1.223c0.2,3.235,2.798,5.833,6.033,6.032v1.224c0,0.222,0.182,0.403,0.403,0.403s0.403-0.182,0.403-0.403v-1.224c3.235-0.199,5.833-2.797,6.032-6.032h1.224c0.222,0,0.403-0.182,0.403-0.403S17.881,9.597,17.659,9.597 M14.435,10.403h1.193c-0.198,2.791-2.434,5.026-5.225,5.225v-1.193c0-0.222-0.182-0.403-0.403-0.403s-0.403,0.182-0.403,0.403v1.193c-2.792-0.198-5.027-2.434-5.224-5.225h1.193c0.222,0,0.403-0.182,0.403-0.403S5.787,9.597,5.565,9.597H4.373C4.57,6.805,6.805,4.57,9.597,4.373v1.193c0,0.222,0.182,0.403,0.403,0.403s0.403-0.182,0.403-0.403V4.373c2.791,0.197,5.026,2.433,5.225,5.224h-1.193c-0.222,0-0.403,0.182-0.403,0.403S14.213,10.403,14.435,10.403"></path>
    </svg>
}

export const XIcon = ({size = 24}) => {
    return <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width={size} height={size}>
        <line x1="4" y1="4" x2="20" y2="20" stroke="white" strokeWidth="2" strokeLinecap="round"/>
        <line x1="4" y1="20" x2="20" y2="4" stroke="white" strokeWidth="2" strokeLinecap="round"/>
    </svg>
}

export const GearIcon = ({size = 24}) => {
    return <svg version="1.1" xmlns="http://www.w3.org/2000/svg"
                width={size} height={size} viewBox="0 0 512 512">
        <path fill="currentColor" d="M496,293.984c9.031-0.703,16-8.25,16-17.297v-41.375c0-9.063-6.969-16.594-16-17.313l-54.828-4.281
                    c-3.484-0.266-6.484-2.453-7.828-5.688l-18.031-43.516c-1.344-3.219-0.781-6.906,1.5-9.547l35.75-41.813
                    c5.875-6.891,5.5-17.141-0.922-23.547l-29.25-29.25c-6.406-6.406-16.672-6.813-23.547-0.922l-41.813,35.75
                    c-2.641,2.266-6.344,2.844-9.547,1.516l-43.531-18.047c-3.219-1.328-5.422-4.375-5.703-7.828l-4.266-54.813
                    C293.281,6.969,285.75,0,276.688,0h-41.375c-9.063,0-16.594,6.969-17.297,16.016l-4.281,54.813c-0.266,3.469-2.469,6.5-5.688,7.828
                    l-43.531,18.047c-3.219,1.328-6.906,0.75-9.563-1.516l-41.797-35.75c-6.875-5.891-17.125-5.484-23.547,0.922l-29.25,29.25
                    c-6.406,6.406-6.797,16.656-0.922,23.547l35.75,41.813c2.25,2.641,2.844,6.328,1.5,9.547l-18.031,43.516
                    c-1.313,3.234-4.359,5.422-7.813,5.688L16,218c-9.031,0.719-16,8.25-16,17.313v41.359c0,9.063,6.969,16.609,16,17.313l54.844,4.266
                    c3.453,0.281,6.5,2.484,7.813,5.703l18.031,43.516c1.344,3.219,0.75,6.922-1.5,9.563l-35.75,41.813
                    c-5.875,6.875-5.484,17.125,0.922,23.547l29.25,29.25c6.422,6.406,16.672,6.797,23.547,0.906l41.797-35.75
                    c2.656-2.25,6.344-2.844,9.563-1.5l43.531,18.031c3.219,1.344,5.422,4.359,5.688,7.844l4.281,54.813
                    c0.703,9.031,8.234,16.016,17.297,16.016h41.375c9.063,0,16.594-6.984,17.297-16.016l4.266-54.813
                    c0.281-3.484,2.484-6.5,5.703-7.844l43.531-18.031c3.203-1.344,6.922-0.75,9.547,1.5l41.813,35.75
                    c6.875,5.891,17.141,5.5,23.547-0.906l29.25-29.25c6.422-6.422,6.797-16.672,0.922-23.547l-35.75-41.813
                    c-2.25-2.641-2.844-6.344-1.5-9.563l18.031-43.516c1.344-3.219,4.344-5.422,7.828-5.703L496,293.984z M256,342.516
                    c-23.109,0-44.844-9-61.188-25.328c-16.344-16.359-25.344-38.078-25.344-61.203c0-23.109,9-44.844,25.344-61.172
                    c16.344-16.359,38.078-25.344,61.188-25.344c23.125,0,44.844,8.984,61.188,25.344c16.344,16.328,25.344,38.063,25.344,61.172
                    c0,23.125-9,44.844-25.344,61.203C300.844,333.516,279.125,342.516,256,342.516z"/>
    </svg>
}

export const ArrowLeftIcon = ({size}) => {
    return <svg className="w-6 h-6 text-gray-800 dark:text-white" aria-hidden="true" xmlns="http://www.w3.org/2000/svg"
                width={size} height={size} fill="none" viewBox="0 0 24 24">
        <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M5 12h14M5 12l4-4m-4 4 4 4"/>
    </svg>
}

export const GlobeIcon = ({size}) => {
    return <svg width={size} height={size} viewBox="0 0 55.818 55.818" xmlns="http://www.w3.org/2000/svg">
        <g data-name="Group 6" transform="translate(-1212.948 -289.602)">
            <path id="Path_19" data-name="Path 19"
                  d="M1249.54,294.79s-4.5.25-5,6.25a17.908,17.908,0,0,0,2.5,10.5s2.193-1.558-.028,5.971,7.278,14.529,10.778,6.279-.5-11.783,2-12.641a33.771,33.771,0,0,0,5.382-2.6l-3.229-6.081-5.21-5.421-7.43-4.027Z"
                  fill="currentColor"></path>
            <path id="Path_20" data-name="Path 20"
                  d="M1219.365,331.985s2.675-14.195,6.425-10.695.25,5.5,2.5,9,5.25,1.5,5.5,5.5.755,6.979,2.618,7.241S1222.967,339.984,1219.365,331.985Z"
                  fill="currentColor"></path>
            <path id="Path_21" data-name="Path 21"
                  d="M1266.766,317.511a25.909,25.909,0,1,1-25.91-25.909A25.909,25.909,0,0,1,1266.766,317.511Z"
                  fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round"
                  stroke-width="4"></path>
            <path id="Path_22" data-name="Path 22"
                  d="M1240.122,311.619a6.078,6.078,0,1,1-6.078-6.079A6.079,6.079,0,0,1,1240.122,311.619Z"
                  fill="currentColor"></path>
        </g>
    </svg>
}

export const AsteroidIcon = ({size}) => {
    return <svg width={size} height={size} viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
        <g>
            <path
                d="M12 2C6.47715 2 2 6.47715 2 12C2 12.4447 2.02903 12.8826 2.0853 13.312C2.61321 17.3405 5.53889 20.6144 9.38261 21.654C10.2169 21.8796 11.0944 22 12 22C16.8786 22 20.9413 18.5064 21.8227 13.8845C21.9391 13.2742 22 12.6442 22 12C22 8.87326 20.565 6.08169 18.3176 4.24796C16.5954 2.84273 14.3961 2 12 2Z"
                stroke="currentColor"></path>
            <path opacity="1"
                  d="M2.08545 13.312C2.68675 13.1097 3.33065 13 4.00015 13C7.31386 13 10.0002 15.6863 10.0002 19C10.0002 19.9529 9.77804 20.8538 9.38276 21.654"
                  stroke="currentColor"></path>
            <path opacity="1"
                  d="M21.8227 13.8846C19.0727 13.3375 17 10.9108 17 8.00008C17 6.58023 17.4932 5.27556 18.3176 4.24805"
                  stroke="currentColor"></path>
            <path
                d="M16 16C16 16.5523 15.5523 17 15 17C14.4477 17 14 16.5523 14 16C14 15.4477 14.4477 15 15 15C15.5523 15 16 15.4477 16 16Z"
                stroke="currentColor"></path>
            <path
                d="M13 8.5C13 9.88071 11.8807 11 10.5 11C9.11929 11 8 9.88071 8 8.5C8 7.11929 9.11929 6 10.5 6C11.8807 6 13 7.11929 13 8.5Z"
                stroke="currentColor"></path>
        </g>
    </svg>
}