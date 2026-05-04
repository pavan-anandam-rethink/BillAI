export class DeleteDialog {
  constructor(public opened: boolean,
    public title: string,
    public message: string,
    public width: number = 450,
    public height: number = 150,
    public paddingBottom: string = '55vh',
    public confirmButtonText: string = 'Delete',
    public cancelButtonText: string = 'Cancel'
    ) { }
}
