import { CheckboxData } from "mit-mainpanel/checkbox/cbData";

export class FoldoutCBData {
    Title: FOTitleCBData;
    Entries: FOEntryCBData[];

    constructor(title: FOTitleCBData, entries: FOEntryCBData[]) {
        this.Title = title;
        this.Entries = entries;
    }
}

export class FOTitleCBData
{
    Id : string;
    Label : string;
    Tooltip : string;
    Checkbox : CheckboxData;

    constructor(id : string, label : string, tooltip : string, checkbox : CheckboxData)
    {
        this.Id = id;
        this.Label = label;
        this.Tooltip = tooltip;
        this.Checkbox = checkbox;
    }
}

export class FOEntryCBData
{
    Id : string;
    Label : string;
    Tooltip : string;
    Checkbox : CheckboxData;

    constructor(id : string, label : string, tooltip : string, checkbox : CheckboxData)
    {
        this.Id = id;
        this.Label = label;
        this.Tooltip = tooltip;
        this.Checkbox = checkbox;
    }
}
