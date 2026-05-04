import { Component, Input } from "@angular/core";
import {NotificationTypes} from "@core/enums/common";

@Component({
    selector: 'notify-component',
    templateUrl: './notify.component.html',
    styleUrls: ['./notify.component.css']
})  

export class NotifyComponent {
   @Input() notificationText: string;
   @Input() type: NotificationTypes;
}