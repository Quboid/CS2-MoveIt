export class CheckboxState
{
    Id : string;
    /// <summary>
    /// IsEnabled: is this widget able to be interacted with?
    /// </summary>
    IsEnabled : boolean;
    /// <summary>
    /// IsActive: is this widget ticked/open/etc?
    /// </summary>
    IsActive : boolean;

    constructor(id : string, active : boolean, enabled : boolean)
    {
        this.Id = id;
        this.IsActive = active;
        this.IsEnabled = enabled;
    }
}
