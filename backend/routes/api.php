<?php

use App\Http\Controllers\Api\V1\EmpathiaController;
use App\Http\Middleware\AuthenticateApiToken;
use Illuminate\Support\Facades\Route;

Route::get('/health', [EmpathiaController::class, 'health']);

Route::post('/auth/login', [EmpathiaController::class, 'login']);

Route::middleware(AuthenticateApiToken::class)->group(function () {
    Route::post('/auth/logout', [EmpathiaController::class, 'logout']);
    Route::get('/auth/me', [EmpathiaController::class, 'me']);

    Route::post('/accompaniment/sessions', [EmpathiaController::class, 'createSession']);
    Route::get('/accompaniment/sessions/{sessionId}', [EmpathiaController::class, 'getSession']);
    Route::post('/accompaniment/sessions/{sessionId}/close', [EmpathiaController::class, 'closeSession']);
    Route::post('/accompaniment/sessions/{sessionId}/turns', [EmpathiaController::class, 'createTurn']);
    Route::get('/accompaniment/sessions/{sessionId}/events', [EmpathiaController::class, 'events']);
    Route::get('/accompaniment/turns/{turnId}/audio/tts', [EmpathiaController::class, 'ttsAudio']);

    Route::get('/risk-signals', [EmpathiaController::class, 'riskSignals']);
    Route::get('/risk-catalog', [EmpathiaController::class, 'riskCatalog']);
    Route::get('/students', [EmpathiaController::class, 'students']);
});
