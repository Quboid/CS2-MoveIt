import styles from "./panel.module.scss";

function DropdownParentRowPressed(id: string) {
    console.log(`MoveIt DD ${id}`);
}

export function DropdownParentRow(label: string, id: string) {
    const classes: string = `${styles.row} ${styles.dropdownRow}`;

    return (
        <div className={classes} onSelect={() => DropdownParentRowPressed(id)}>{label}</div>
    );
}
