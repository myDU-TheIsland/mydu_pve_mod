if (!window.global_resources) {
    window.global_resources = {};
}

if (!window.page) {
    window.page = "player";
}

let modApi = {};
modApi.cb = (data) => {
    console.log("Mod.DynamicEncounters", 1999999, [], JSON.stringify(data));
};

modApi.setWaypoint = (pos) => {
    console.log('setWaypoint', pos);
};

modApi.saveGuiPosition = (pos) => {
    console.log('saveGuiPosition', pos);
};

modApi.setPage = (page) => {
    window.page = page;
};

modApi.imageUrl = (path) => {
    return path;
}

modApi.getPlayerInfo = () => {
    return {
        "_playerId": 10000,
        "skinIcon": "stubs/chara_male-military.png",
        "playerName": "VoidRunner7891",
        "apheliaId": 2,
        "onPlayerInfoUpdate": {
            "_areEventVoided": false,
            "_listeners": [
                null,
                null,
                null,
                null
            ]
        }
    };
}

modApi.setContext = (data) => {
    window.player_context = {
        ...window.player_context || {},
        ...data
    };
};

modApi.refreshNpcQuestList = () => {
    console.log('refreshNpcQuestList');
};

modApi.refreshPlayerQuestList = () => {
    console.log('refreshPlayerQuestList');
};

modApi.refreshPlayerPartyData = () => {
    console.log('refreshPlayerPartyData');
};

modApi.acceptQuest = (questId) => {
    console.log('acceptQuest', window.player_context, questId);
};

modApi.abandonQuest = (questId) => {
    console.log('abandonQuest', window.player_context, questId);
};

modApi.createGroup = () => {
    console.log('createGroup');
};

modApi.setResourceContents = (name, contentType, contents) => {
    const blob = new Blob([contents], {type: contentType});
    const blobUrl = URL.createObjectURL(blob);
    window.global_resources[name] = blobUrl;

    modApi.cb(`Set Resource ${name} as ${contentType} to ${blobUrl}`);

    return window.global_resources[name];
};

modApi.appendIframe = (id, url) => {
    const iframe = document.createElement('iframe');

    iframe.id = id;
    iframe.className = "frame";
    iframe.src = url;
    iframe.width = '800';
    iframe.height = '600';
    iframe.style.border = 'none';

    document.body.appendChild(iframe);

    return iframe;
};

modApi.setPlayerLocation = (playerId) => {
    console.log('setPlayerLocation', playerId);
};

modApi.leaveGroup = () => {
    console.log('leaveGroup');
};

modApi.disbandGroup = () => {
    console.log('disbandGroup');
};

modApi.cancelInvite = (playerId) => {
    console.log('cancelInvite', playerId);
};

modApi.acceptInvite = () => {
    console.log('acceptInvite');
};

modApi.acceptRequest = (playerId) => {
    console.log('acceptRequest', playerId);
};

modApi.rejectRequest = (playerId) => {
    console.log('rejectRequest', playerId);
};

modApi.setRole = (role) => {
    console.log('setRole', role);
};

modApi.inviteToGroup = (playerName) => {
    console.log('inviteToGroup', playerName);
};

modApi.addInlineCss = (cssContent) => {
    let style = document.createElement("style");
    style.type = "text/css";
    style.innerHTML = cssContent;
    document.head.appendChild(style);
};

modApi.addJsUrl = (src) => {
    let script = document.createElement("script");
    script.src = src;
    document.body.appendChild(script);
};

modApi.addJs = (type, text) => {
    let script = document.createElement("script");
    script.type = type;
    script.textContent = text;
    document.body.appendChild(script);
};

modApi.removeAppRoot = () => {
    document.getElementById("root").remove();
};

window.modApi = modApi;
