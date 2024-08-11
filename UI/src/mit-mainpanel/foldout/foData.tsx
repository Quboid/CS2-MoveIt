import { CheckboxData } from "mit-mainpanel/checkbox/cbData";

// Main Foldout menu

export class FoldoutData {
    Title: FOTitleData;
    Entries: FOMainEntryData[];

    constructor(title: FOTitleData, entries: FOMainEntryData[]) {
        this.Title = title;
        this.Entries = entries;
    }
}

export class FOTitleData
{
    Section : string;
    Id : string;
    Label : string;
    Tooltip : string;
    Checkbox? : CheckboxData;
    CBWhenClosed? : boolean;
    CBWhenOpen? : boolean;

    constructor(section : string, id : string, label : string, tooltip : string, checkbox? : CheckboxData, cbWhenClosed? : boolean, cbWhenOpen? : boolean)
    {
        this.Section = section;
        this.Id = id;
        this.Label = label;
        this.Tooltip = tooltip;
        this.Checkbox = checkbox;
        this.CBWhenClosed = cbWhenClosed;
        this.CBWhenOpen = cbWhenOpen;
    }
}

export class FOEntryDataBase
{
    Section : string;
    Id : string;
    RawId : string;
    Label : string;
    Tooltip : string;
    Checkbox? : CheckboxData;

    constructor(section : string, rawId : string, label : string, tooltip : string, checkbox? : CheckboxData)
    {
        this.Section = section;
        this.Id = rawId + "Row";
        this.RawId = rawId;
        this.Label = label;
        this.Tooltip = tooltip;
        this.Checkbox = checkbox;
    }
}

export class FOMainEntryData extends FOEntryDataBase
{
    Popout? : FOPopoutData;

    constructor(section : string, rawId : string, label : string, tooltip : string, checkbox? : CheckboxData, popout? : FOPopoutData)
    {
        super(section, rawId, label, tooltip, checkbox);
        this.Popout = popout;
    }
}

// Popout submenu

export class FOPopoutData
{
    Entries: FOPopoutEntryData[];

    constructor(entries: FOPopoutEntryData[]) {
        this.Entries = entries;
    }
}

export class FOPopoutEntryData extends FOEntryDataBase
{
    SubSection : string;

    constructor(section : string, subSection : string, rawId : string, label : string, tooltip : string, checkbox? : CheckboxData)
    {
        super(section, rawId, label, tooltip, checkbox);
        this.SubSection = subSection;
    }
}
