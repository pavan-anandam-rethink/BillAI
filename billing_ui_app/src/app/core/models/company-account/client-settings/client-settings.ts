import { NoteType, CustomField } from '../../common';
import { ClientStatus } from './client-status';

export class ClientSettings {
    clientStatuses: ClientStatus[];
    noteTypes: NoteType[];
    customFields: CustomField[]
}