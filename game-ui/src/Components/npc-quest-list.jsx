import {useEffect, useState} from "react";
import {CloseButton, Container, Header, Panel, PanelBody, Title, Tab} from "./panel";
import styled from "styled-components";
import QuestItem from "./quest-item";
import {IconButton, PrimaryButton} from "./buttons";
import {RefreshIcon} from "./icons";

const CategoryPanel = styled.div`
    background-color: rgb(13, 24, 28);
    min-width: 200px;
    display: flex;
    align-items: start;
`;

const TabContainer = styled.div`
    width: 100%;
    padding: 8px;

    * {
        margin-bottom: 2px;
    }
`;

const ContentPanel = styled.div`
    padding: 16px;
    flex-grow: 1;
`;

const NpcQuestList = (props) => {

    const [jobs, setJobs] = useState([]);
    const [acceptedJobs, setAcceptedJobs] = useState([]);
    const [expandedMap, setExpandedMap] = useState({});
    const [factionName, setFactionName] = useState("Unknown");
    const [factionId, setFactionId] = useState(0);
    const [constructId, setConstructId] = useState(0);
    const [error, setError] = useState("");
    const [acceptedQuestMap, setAcceptedQuestMap] = useState({});
    const [typeFilter, setTypeTypeFilter] = useState([]);

    useEffect(() => {

        if (!window.global_resources) {
            return;
        }

        fetchData();
        fetchAcceptedData();

    }, []);

    const fetchAcceptedData = () => {

        try {
            if (!window.global_resources) {
                setError("Null Global Resources");
                return;
            }

            let url = window.global_resources["player-quests"];

            fetch(url)
                .then(res => {
                    if (!res.ok) {
                        setError(`Failed HTTP Status: ${res.status}`);
                        return [];
                    }

                    return res.json();
                }, err => {
                    window.modApi.cb(`Caught Error ${err}`);
                })
                .then(data => {

                    if (!data) {
                        window.modApi.cb(`Invalid Data`);
                        return;
                    }

                    setAcceptedJobs(data.jobs);
                }, err => {
                    window.modApi.cb(`Caught Error 2 ${err}`);
                });
        } catch (e) {
            setError(`Thrown Error ${e}`);
            window.modApi.cb(`Thrown Error ${e}`);
        }
    };

    const fetchData = () => {

        try {
            if (!window.global_resources) {
                setError("Null Global Resources");
                return;
            }

            let url = window.global_resources["faction-quests"];

            fetch(url)
                .then(res => {
                    if (!res.ok) {
                        setError(`Failed HTTP Status: ${res.status}`);
                        return [];
                    }

                    return res.json();
                }, err => {
                    window.modApi.cb(`Caught Error ${err}`);
                })
                .then(data => {

                    if (!data) {
                        window.modApi.cb(`Invalid Data`);
                        return;
                    }

                    setConstructId(data.constructId);
                    setJobs(data.jobs);
                    setFactionName(data.faction);
                    setFactionId(data.factionId);
                }, err => {
                    window.modApi.cb(`Caught Error 2 ${err}`);
                });
        } catch (e) {
            setError(`Thrown Error ${e}`);
            window.modApi.cb(`Thrown Error ${e}`);
        }
    };

    const handleClose = () => {
        document.getElementById("root").remove();
    };

    const handleSelect = (item) => {
        if (expandedMap[item.id] === true) {
            setExpandedMap({});
        } else {
            setExpandedMap({[item.id]: true});
        }
    };

    const handleAccepted = (index, questId) => {
        setAcceptedQuestMap(Object.assign({[questId]: true}, acceptedQuestMap));

        handleRefresh();
    };

    const filterJobs = (items, filters) => {
        if (filters.length === 0) {
            return items;
        }

        if (filters.length === 1 && filters[0] === "accepted")
        {
            return acceptedJobs.reduce((res, item) => ([
                ...res,
                {
                    ...item,
                    accepted: true,
                },
            ]), []);
        }

        return items.filter(r => filters.includes(r.type));
    };

    const questItems = filterJobs(jobs, typeFilter)
        .map((item, index) =>
            <QuestItem key={index}
                       onSelect={() => handleSelect(item)}
                       questId={item.id}
                       rewards={item.rewards}
                       title={item.title}
                       tasks={item.tasks}
                       type={item.type}
                       safe={item.safe}
                       canAccept={true}
                       onAccepted={(questId) => handleAccepted(index, questId)}
                       accepted={acceptedQuestMap[item.id] || item.accepted}
                       expanded={expandedMap[item.id]}/>
        );

    const handleRefresh = () => {
        window.modApi.refreshNpcQuestList();

        setTimeout(() => {
            fetchData();
            fetchAcceptedData();
        }, 1000);
    };

    const handlePickupDeliver = () => {
        window.modApi.interact(constructId);
    };

    const countRequisition = () => {
        return jobs.filter(r => r.type === "order").length;
    };

    const countDelivery = () => {
        return jobs.filter(r => r.type === "transport" || r.type === "reverse-transport").length;
    };

    const countAccepted = () => {
        return acceptedJobs.length;
    };

    const clearFilters = () => {
        setTypeTypeFilter([]);
    };

    const setTransportFilter = () => {
        setTypeTypeFilter(["transport", "reverse-transport"]);
    };

    const setAcceptedFilter = () => {
        setTypeTypeFilter(["accepted"]);
    };

    const setOrderFilter = () => {
        setTypeTypeFilter(["order"]);
    };

    const getFilterId = () => {
        return typeFilter.join("-");
    }

    return (
        <Container>
            <Panel>
                <Header>
                    <Title>{factionName} Faction board</Title>
                    <IconButton onClick={handleRefresh}><RefreshIcon/></IconButton>
                    &nbsp;&nbsp;&nbsp;&nbsp;
                    <PrimaryButton onClick={handlePickupDeliver}>Pickup/Deliver</PrimaryButton>
                    <CloseButton onClick={handleClose}>&times;</CloseButton>
                </Header>
                <PanelBody>
                    <CategoryPanel>
                        <TabContainer>
                            <Tab onClick={() => clearFilters()} selected={!getFilterId()}>Missions ({jobs.length})</Tab>
                            <Tab onClick={() => setOrderFilter()} selected={getFilterId() === "order"}>Orders ({countRequisition()})</Tab>
                            <Tab onClick={() => setTransportFilter()} selected={getFilterId() === "transport-reverse-transport"}>Deliveries ({countDelivery()})</Tab>
                            <br/>
                            <Tab onClick={() => setAcceptedFilter()} selected={getFilterId() === "accepted"}>Accepted ({countAccepted()})</Tab>
                            {/*<Tab>Combat</Tab>*/}
                            {/*<Tab>Package Delivery</Tab>*/}
                            <br/>
                            <Tab>Reputation</Tab>
                            <Tab>Faction Info</Tab>
                        </TabContainer>
                    </CategoryPanel>
                    <ContentPanel>
                        <div>{error}</div>
                        {questItems}
                    </ContentPanel>
                </PanelBody>
            </Panel>
        </Container>
    );
}

export default NpcQuestList;