{{- define "billingservice.fullname" -}}
{{- printf "%s-billingservice" .Release.Name | trunc 63 | trimSuffix "-" -}}
{{- end -}}
