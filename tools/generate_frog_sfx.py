#!/usr/bin/env python3
"""Generate original 8-bit frog tongue attack sound effects."""

import math
import random
import struct
import wave
from pathlib import Path


SAMPLE_RATE = 44100
OUTPUT_DIR = Path(__file__).resolve().parents[1] / "sound" / "frog_attack"


def smoothstep(value):
    value = max(0.0, min(1.0, value))
    return value * value * (3.0 - 2.0 * value)


def note_voice(time_in_note, duration, start_frequency, end_frequency, duty=0.30):
    progress = time_in_note / duration
    frequency = start_frequency + (end_frequency - start_frequency) * smoothstep(progress)

    # Integrating this approximate frequency is enough for the tiny pitch scoops here.
    phase = 2.0 * math.pi * frequency * time_in_note
    cycle = (phase / (2.0 * math.pi)) % 1.0
    pulse = 1.0 if cycle < duty else -1.0
    triangle = 1.0 - 4.0 * abs(cycle - 0.5)

    attack = min(1.0, time_in_note / 0.003)
    decay = (1.0 - progress) ** 0.72
    return (0.78 * pulse + 0.22 * triangle) * attack * decay


def build_sound(notes, hit):
    random.seed(20260726 + int(hit))
    note_duration = 0.072
    note_gap = 0.006
    tail_duration = 0.055 if hit else 0.042
    total_duration = len(notes) * note_duration + (len(notes) - 1) * note_gap + tail_duration
    sample_count = int(total_duration * SAMPLE_RATE)
    samples = []
    previous_noise = 0.0

    for index in range(sample_count):
        time = index / SAMPLE_RATE
        value = 0.0

        # A short, filtered noise snap suggests the tongue leaving the mouth.
        raw_noise = random.uniform(-1.0, 1.0)
        previous_noise = 0.68 * previous_noise + 0.32 * raw_noise
        if time < 0.018:
            value += previous_noise * 0.38 * (1.0 - time / 0.018)

        for note_index, frequency in enumerate(notes):
            note_start = note_index * (note_duration + note_gap)
            local_time = time - note_start
            if 0.0 <= local_time < note_duration:
                # A tiny upward scoop makes the sound feel elastic.
                scoop_start = frequency * (0.90 if note_index == 0 else 0.96)
                value += 0.70 * note_voice(
                    local_time, note_duration, scoop_start, frequency, duty=0.28
                )

        notes_end = len(notes) * note_duration + (len(notes) - 1) * note_gap
        if time >= notes_end:
            tail_time = time - notes_end
            tail_progress = tail_time / tail_duration
            if hit:
                # Bright octave sparkle for a successful impact.
                sparkle_frequency = notes[-1] * (1.0 + 0.03 * tail_progress)
                sparkle_cycle = (sparkle_frequency * tail_time) % 1.0
                sparkle = 1.0 if sparkle_cycle < 0.20 else -1.0
                value += sparkle * 0.34 * (1.0 - tail_progress) ** 1.8
                value += previous_noise * 0.20 * (1.0 - tail_progress) ** 2.5
            else:
                # Softer recoil tick when the tongue catches nothing.
                recoil_frequency = notes[-1] * (1.0 - 0.18 * tail_progress)
                recoil_cycle = (recoil_frequency * tail_time) % 1.0
                recoil = 1.0 if recoil_cycle < 0.16 else -1.0
                value += recoil * 0.18 * (1.0 - tail_progress) ** 2.2

        # Eight-bit amplitude quantization gives the waveform a pixel-game edge.
        value = max(-1.0, min(1.0, value))
        value = round(value * 63.0) / 63.0
        samples.append(value)

    peak = max(abs(sample) for sample in samples) or 1.0
    return [sample * 0.92 / peak for sample in samples]


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
        "frog_tongue_miss_do_sol.wav": build_sound([523.251, 783.991], hit=False),
        "frog_tongue_hit_do_high_do.wav": build_sound([523.251, 1046.502], hit=True),
    }
    for filename, samples in sounds.items():
        destination = OUTPUT_DIR / filename
        write_wav(destination, samples)
        print(destination)


if __name__ == "__main__":
    main()
