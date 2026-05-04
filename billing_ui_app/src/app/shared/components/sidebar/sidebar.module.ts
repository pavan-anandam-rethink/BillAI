import { NgModule } from '@angular/core'
import { CommonModule } from '@angular/common'
import { SidebarComponent } from './sidebar.component'
import { InsertionDirective } from './insertion.directive'
import { SidebarService } from './sidebar.service'

@NgModule({
    imports: [CommonModule],
    declarations: [SidebarComponent, InsertionDirective],
    providers: [SidebarService]
})
export class SidebarModule { }