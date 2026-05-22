{{- define "ai-rules-engine.name" -}}
ai-rules-engine
{{- end -}}

{{- define "ai-rules-engine.fullname" -}}
{{- printf "%s" (include "ai-rules-engine.name" .) -}}
{{- end -}}
