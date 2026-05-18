{{- define "billingservice.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "billingservice.fullname" -}}
{{- printf "%s-%s" .Release.Name (include "billingservice.name" .) | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "billingservice.labels" -}}
app.kubernetes.io/name: {{ include "billingservice.name" . }}
helm.sh/chart: {{ .Chart.Name }}-{{ .Chart.Version | replace "+" "_" }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end -}}

{{- define "billingservice.selectorLabels" -}}
app.kubernetes.io/name: {{ include "billingservice.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end -}}

{{- define "billingservice.serviceAccountName" -}}
{{- if .Values.serviceAccount.create -}}
{{- default (include "billingservice.fullname" .) .Values.serviceAccount.name -}}
{{- else -}}
{{- default "default" .Values.serviceAccount.name -}}
{{- end -}}
{{- end -}}
