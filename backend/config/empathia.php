<?php

return [
    'intel_stub' => (bool) env('INTEL_STUB', true),
    'intelligence_url' => env('INTELLIGENCE_URL', 'http://192.168.1.55:8100'),
    'intelligence_token' => env('INTEL_INTERNAL_TOKEN', 'empathia-internal-dev-token'),
    'data_root' => env('EMPATHIA_DATA_ROOT', dirname(base_path()).DIRECTORY_SEPARATOR.'datos'),
    'audio_retention' => env('AUDIO_RETENTION', 'R1'),
];
