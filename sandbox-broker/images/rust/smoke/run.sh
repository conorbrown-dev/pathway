#!/bin/sh
# Runs inside the rust image as the learner user (via as-learner), with no network.
set -eu
cd /workspace
cp /smoke/axum_probe.rs main.rs
start=$(date +%s%N)
pathway-rustc main.rs -o main
compiled=$(date +%s%N)
./main
# GitHub Actions turns ::notice lines into annotations, which stay readable without log access.
echo "::notice title=pathway-rustc::Tokio + Axum + SQLite + loopback probe compiled in $(( (compiled - start) / 1000000 )) ms and passed"
echo "::notice title=Prebuilt crates::$(grep -v '^pathway_crates' /opt/pathway-crates/CRATES.txt | tr '\n' ',' | sed 's/,$//; s/,/, /g')"
