import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

@Component({
  selector: 'app-no-auth-dialog',
  templateUrl: './no-auth-dialog.component.html',
  styleUrls: ['./no-auth-dialog.component.css']
})
export class NoAuthDialogComponent {

  constructor(public dialogRef: MatDialogRef<NoAuthDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any) {}

  proceed(tabName: string) {
    this.dialogRef.close(true);
  }

  cancelIncompleteAuth() {
    this.dialogRef.close(false);
  }
}
