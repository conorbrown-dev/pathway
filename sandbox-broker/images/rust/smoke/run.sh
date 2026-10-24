#!/bin/sh
# Runs inside the rust image as the learner user (via as-learner), with no network.
set -eu
cd /workspace
cp /smoke/axum_probe.rs main.rs
start=$(date +%s%N)
pathway-rustc main.rs -o main
compiled=$(date +%s%N)
./main
echo "pathway-rustc compile: $(( (compiled - start) / 1000000 )) ms"
cat /opt/pathway-crates/CRATES.txt
