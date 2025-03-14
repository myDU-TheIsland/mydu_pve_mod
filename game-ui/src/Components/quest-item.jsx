import {
    CheckIcon,
    ExpandIcon,
    FireIcon,
    ReverseTransportIcon,
    ShieldIcon,
    SquareIcon,
    TimedIcon,
    TransportIcon
} from "./icons";
import {PrimaryButton, IconButton, TargetButton, SecondaryButton, DestructiveButton} from "./buttons";
import styled from "styled-components";
import {useState} from "react";

const ExpandItem = styled.div`
    background-color: rgb(27, 48, 56);
    color: rgb(180, 221, 235);
    margin-bottom: 2px;
`;

const Header = styled.div`
    display: flex;
    align-items: center;
    padding: 8px;
    cursor: pointer;

    &:hover {
        background-color: rgb(250, 212, 122);
        color: black;
    }

    &.expanded {
        background-color: rgb(250, 212, 122);
        color: black;
    }
`;

const HeaderText = styled.span`
    padding-left: 4px;
`;

const Contents = styled.div`
    padding: 8px;
    background-color: rgb(13, 24, 28);
`;

const Task = styled.div`
    padding: 8px 0 8px 0;
`;

const ItemContainer = styled.div`
    display: flex;
    align-items: center;
`;

const ItemIcon = styled.div`
    margin-right: 8px;
`;

const ItemText = styled.span`
    margin-right: 8px;
`;

const TaskSubTitle = styled.h3`
    margin: 8px 0 8px 0;
    font-size: 1em;
    font-weight: normal;
`;

const ActionContainer = styled.div`
    display: flex;
    justify-content: start;
`;

const Spacing = styled.div`
    flex-grow: 1;
`;

const AcceptedPill = styled.div`
    text-align: right;
    font-size: .7em;
    padding: .3em;
    background-color: rgb(13, 24, 28);
    border-radius: 2px;
    color: rgb(180, 221, 235);
`;

const ItemList = styled.ul`
    all: unset;
    padding-left: 1.5em;
`;
const ItemListItem = styled.li`
    margin-top: 0.5em;
`;

const QuestItem = ({
                       questId,
                       title,
                       type,
                       safe,
                       tasks,
                       expanded,
                       onSelect,
                       rewards,
                       onAccepted,
                       onAbandon,
                       accepted,
                       canAccept,
                       canAbandon
                   }) => {

    const [confirmAbandon, setConfirmAbandon] = useState(false);

    const setWaypoint = (pos) => {
        window.modApi.setWaypoint(pos);
    };

    const renderItem = (item, index) => {
        return (<ItemListItem key={index}>{item.quantity}x {item.name}</ItemListItem>);
    };

    const renderItems = (items) => {
        if (!items) return null;

        return (
            <ItemList>
                {items.map((r, i) => {
                    return renderItem(r, i);
                })}
            </ItemList>
        );
    };

    const tasksRender = tasks
        .map((t, i) => <Task key={i}>
                <ItemContainer>
                    <ItemIcon><CheckIcon checked={t.status === 'completed'}/></ItemIcon>
                    <ItemText>{t.title}</ItemText><TargetButton onClick={() => setWaypoint(t.position)}/>
                </ItemContainer>
                <ItemContainer>
                    {renderItems(t.items)}
                </ItemContainer>
            </Task>
        );

    const rewardsRender = rewards
        .map((r, i) => <Task key={i}>
                <ItemContainer>
                    <ItemIcon><SquareIcon/></ItemIcon>
                    <ItemText>{r}</ItemText>
                </ItemContainer>
            </Task>
        );

    const acceptQuest = () => {
        window.modApi.acceptQuest(questId);
        onAccepted(questId);
    }

    const abandonQuest = () => {
        window.modApi.abandonQuest(questId);
        onAbandon(questId);
    }

    const showConfirmAbandon = () => {
        setConfirmAbandon(true);

        setTimeout(() => {
            setConfirmAbandon(false);
        }, 3000);
    }

    let headerClassNames = [];
    if (expanded) {
        headerClassNames.push('expanded');
    }
    if (accepted) {
        headerClassNames.push('accepted');
    }

    const QuestIcon = ({type, safe}) => {

        let icons = [];

        if (safe) {
            icons.push(<ShieldIcon/>);
        } else {
            icons.push(<FireIcon/>);
        }

        switch (type) {
            case "transport":
                icons.push(<TransportIcon/>);
                break;
            case "timed-transport":
                icons.push(<TimedIcon/>);
                icons.push(<TransportIcon/>);
                break;
            case "reverse-transport":
                icons.push(<ReverseTransportIcon/>);
                break;
            default:
                break;
        }

        return icons;
    };

    return (
        <ExpandItem>
            <Header onClick={onSelect} className={headerClassNames.join(" ")}>
                <ExpandIcon expanded={expanded}/> <QuestIcon type={type} safe={safe}/> <HeaderText>{title}</HeaderText>
                <Spacing/><AcceptedPill hidden={!accepted}>Accepted</AcceptedPill>
            </Header>
            <Contents hidden={!expanded}>
                <TaskSubTitle>Objectives:</TaskSubTitle>
                {tasksRender}
                <br/>
                <TaskSubTitle>Rewards:</TaskSubTitle>
                {rewardsRender}
                <br/>
                <ActionContainer>
                    <PrimaryButton hidden={accepted || !canAccept} onClick={acceptQuest}>Accept</PrimaryButton>
                    <SecondaryButton hidden={!accepted || !canAccept}>Accepted</SecondaryButton>
                    <PrimaryButton hidden={confirmAbandon || !accepted || !canAbandon}
                                   onClick={showConfirmAbandon}>Abandon</PrimaryButton>
                    {confirmAbandon ?
                        <DestructiveButton onClick={abandonQuest}>Confirm abandon</DestructiveButton> : ""}
                </ActionContainer>
            </Contents>
        </ExpandItem>
    );
}

export default QuestItem;