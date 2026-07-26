#!/usr/bin/env python3
"""Generate original, playback-friendly retro frog tongue sound effects."""

import math
import random
import struct
import wave
from pathlib import Path


SAMPLE_RATE = 48000
OUTPUT_DIR = Path(__file__).resolve().parents[1] / "sound" / "frog_attack"


def smoothstep(value):
    value = max(0.0, min(1.0, value))
    return value * value * (3.0 - 2.0 * value)


def note_voice(time_in_note, duration, start_frequency, end_frequency):
    progress = time_in_note / duration
    frequency = start_frequency + (end_frequency - start_frequency) * smoothstep(progress)

    # Smooth retro tone: rounded pulse plus triangle, without hard discontinuities.
    phase = 2.0 * math.pi * frequency * time_in_note
    sine = math.sin(phase)
    rounded_pulse = math.tanh(1.35 * sine) / math.tanh(1.35)
    triangle = (2.0 / math.pi) * math.asin(sine)

    attack = smoothstep(min(1.0, time_in_note / 0.008))
    release = smoothstep(min(1.0, (duration - time_in_note) / 0.018))
    decay = 1.0 - 0.38 * progress
    return (0.48 * rounded_pulse + 0.52 * triangle) * attack * release * decay


def build_sound(notes, hit):
    random.seed(20260726 + int(hit))
    note_duration = 0.088
    note_gap = 0.010
    tail_duration = 0.062 if hit else 0.050
    total_duration = len(notes) * note_duration + (len(notes) - 1) * note_gap + tail_duration
    sample_count = int(total_duration * SAMPLE_RATE)
    samples = []
    low_pass_state = 0.0

    for index in range(sample_count):
        time = index / SAMPLE_RATE
        value = 0.0

        # A quiet downward "blip" suggests the tongue leaving the mouth.
        if time < 0.032:
            motion_progress = time / 0.032
            motion_frequency = 690.0 - 250.0 * smoothstep(motion_progress)
            motion_envelope = math.sin(math.pi * motion_progress) ** 2
            value += math.sin(2.0 * math.pi * motion_frequency * time) * 0.16 * motion_envelope

        for note_index, frequency in enumerate(notes):
            note_start = note_index * (note_duration + note_gap)
            local_time = time - note_start
            if 0.0 <= local_time < note_duration:
                scoop_start = frequency * (0.94 if note_index == 0 else 0.98)
                value += 0.62 * note_voice(local_time, note_duration, scoop_start, frequency)

        notes_end = len(notes) * note_duration + (len(notes) - 1) * note_gap
        if time >= notes_end:
            tail_time = time - notes_end
            tail_progress = tail_time / tail_duration
            if hit:
                # A soft, bright confirmation ping for a successful impact.
                sparkle = math.sin(2.0 * math.pi * notes[-1] * 1.5 * tail_time)
                value += sparkle * 0.19 * math.sin(math.pi * tail_progress) * (1.0 - tail_progress)
            else:
                # Gentle recoil when the tongue catches nothing.
                recoil_frequency = notes[-1] * (1.0 - 0.10 * tail_progress)
                recoil = math.sin(2.0 * math.pi * recoil_frequency * tail_time)
                value += recoil * 0.10 * math.sin(math.pi * tail_progress) * (1.0 - tail_progress)

        # One-pole low-pass filtering removes harsh digital edges.
        low_pass_state += 0.24 * (value - low_pass_state)
        value = math.tanh(low_pass_state * 1.10) / math.tanh(1.10)

        # Mild 12-bit quantization retains a retro character without sounding broken.
        value = round(max(-1.0, min(1.0, value)) * 2047.0) / 2047.0
        samples.append(value)

    peak = max(abs(sample) for sample in samples) or 1.0
    return [sample * 0.70 / peak for sample in samples]


def write_wav(path, samples):
    path.parent.mkdir(parents=True, exist_ok=True)
    frames = b"".join(
        struct.pack("<h", int(max(-1.0, min(1.0, sample)) * 32767))
        for sample in samples
    )
    with wave.open(str(path), "wb") as wav_file:
        wav_file.setnchannels(1)
        wav_file.setsampwidth(2)
        wav_file.setframerate(SAMPLE_RATE)
        wav_file.writeframes(frames)


def main():
    # C5 → G5: miss; C5 → C6: hit.
    sounds = {
        "frog_tongue_miss_smooth.wav": build_sound([523.251, 783.991], hit=False),
        "frog_tongue_hit_smooth.wav": build_sound([523.251, 1046.502], hit=True),
    }
    for filename, samples in sounds.items():
        destination = OUTPUT_DIR / filename
        write_wav(destination, samples)
        print(destination)


if __name__ == "__main__":
    main()
