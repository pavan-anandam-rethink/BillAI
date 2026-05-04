import { Injectable, EventEmitter} from '@angular/core';
import { PrintComponent, PrintModel } from '.';

@Injectable({ providedIn: 'root' })
export class PrintModalService {
    private modals: PrintComponent[] = [];
    public onClose = new EventEmitter();

    add(modal: PrintComponent) {
        this.modals.push(modal);
    }

    remove(id: string) {
        this.modals = this.modals.filter(x => x.id !== id);
    }

    open(id: string, model: PrintModel) {
        const modal = this.modals.filter(x => x.id === id)[0];
        modal.open(model);
        return modal;
    }

    close(id: string) {
        const modal = this.modals.filter(x => x.id === id)[0];
        modal.close();
    }


}