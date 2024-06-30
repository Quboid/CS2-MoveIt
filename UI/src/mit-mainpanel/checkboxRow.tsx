import styles from "./panel.module.scss";

function CheckboxRowPressed(id: string) {
    console.log(`MoveIt CB ${id}`);
}

export function CheckboxRow(label: string, id: string) {
    const classes: string = `${styles.row} ${styles.checkboxRow}`;

    return (
        <div className={classes} onSelect={() => CheckboxRowPressed(id)}>{label}</div>
    );
}
