# M9.3 evaluation-only third-party notices

This directory supports the isolated M9.3 retrieval evaluation. It is not a production dependency manifest. Approval is based on declared upstream licenses, not formal legal review.

## Frozen model

- Model: `sentence-transformers/all-MiniLM-L6-v2` (Qdrant ONNX export `qdrant/all-MiniLM-L6-v2-onnx`).
- Mirror: `sspcodeflix/textembedding-model-weights` at commit `70df87c74905102ee14f2ee3ae33ccb5cb6a4825`.
- Declared license: Apache-2.0. Text: `licenses/all-minilm-l6-v2-LICENSE`.
- Provenance qualification: the payload was verified by the user against the official Qdrant artifact checksums; the mirror commit history was not independently authenticated.

## Python packages

Exact versions and wheel SHA-256 hashes are in `dependencies.json` and `requirements.lock`. License texts copied from the installed distributions are under `licenses/`.

| Package | Version | Declared license | License files |
|---|---|---|---|
| anyio | 4.16.0 | MIT | `licenses/anyio/LICENSE` |
| certifi | 2026.10.23 | MPL-2.0 | `licenses/certifi/LICENSE` |
| click | 8.5.0 | BSD-3-Clause | `licenses/click/LICENSE.txt` |
| coloredlogs | 15.0.1 | MIT | `licenses/coloredlogs/LICENSE.txt` |
| filelock | 4.2.6 | MIT | `licenses/filelock/LICENSE` |
| flatbuffers | 25.12.19 | Apache 2.0 | `licenses/flatbuffers/LICENSE` |
| fsspec | 2026.9.0 | BSD-3-Clause | `licenses/fsspec/LICENSE` |
| h11 | 0.16.0 | MIT | `licenses/h11/LICENSE.txt` |
| hf-xet | 1.8.0 | Apache-2.0 | `licenses/hf-xet/LICENSE` |
| httpcore | 1.0.9 | BSD-3-Clause | `licenses/httpcore/LICENSE.md` |
| httpx | 0.28.1 | BSD-3-Clause | `licenses/httpx/LICENSE.md` |
| huggingface_hub | 1.33.0 | Apache-2.0 | `licenses/huggingface-hub/LICENSE` |
| humanfriendly | 10.0 | MIT | `licenses/humanfriendly/LICENSE.txt` |
| idna | 3.20 | BSD-3-Clause | `licenses/idna/LICENSE.md` |
| mpmath | 1.3.0 | BSD | `licenses/mpmath/LICENSE` |
| numpy | 2.3.4 | Copyright (c) 2005-2025, NumPy Developers. | `licenses/numpy/LICENSE.txt`, `licenses/numpy/ma-LICENSE`, `licenses/numpy/random-LICENSE.md`, `licenses/numpy/random-include-LICENSE.txt` |
| onnxruntime | 1.23.2 | MIT License | `licenses/onnxruntime/LICENSE`, `licenses/onnxruntime/ThirdPartyNotices.txt` |
| packaging | 26.3 | Apache-2.0 OR BSD-2-Clause | `licenses/packaging/LICENSE`, `licenses/packaging/LICENSE.APACHE`, `licenses/packaging/LICENSE.BSD` |
| protobuf | 7.36.2 | 3-Clause BSD License | `licenses/protobuf/LICENSE` |
| PyYAML | 6.0.3 | MIT | `licenses/pyyaml/LICENSE` |
| sympy | 1.14.0 | BSD | `licenses/sympy/LICENSE` |
| tokenizers | 0.22.1 |  | `licenses/tokenizers/LICENSE` |
| tqdm | 4.70.1 | MPL-2.0 AND MIT | `licenses/tqdm/LICENCE` |
| typing_extensions | 4.16.0 | PSF-2.0 | `licenses/typing-extensions/LICENSE` |

## NumPy bundled-component inspection

The installed `numpy==2.3.4` manylinux wheel was inspected directly. Its principal BSD-3-Clause license does not cover every bundled binary by itself. The wheel bundles:

- `numpy.libs/libscipy_openblas64_-8fb3d286.so` (OpenBLAS, BSD-3-Clause; includes LAPACK contributions).
- `numpy.libs/libgfortran-040039e1-0352e75f.so.5.0.0` (GCC Fortran runtime, GPL-3.0-with-GCC-exception-3.1).
- `numpy.libs/libquadmath-96973f99-934c22de.so.0.0.0` (GCC quadmath runtime, LGPL-2.1-or-later).
- `numpy/random` distribution code (BSD-3-Clause plus MIT/PCG contributions; see `licenses/numpy/random-LICENSE.md` and `licenses/numpy/random-include-LICENSE.txt`).
- `numpy/ma` (masked-array package) has its own BSD-style license in `licenses/numpy/ma-LICENSE`.

The full NumPy distribution license text, including bundled-component notices, is preserved at `licenses/numpy/LICENSE.txt`.

## ONNX Runtime bundled components

`onnxruntime==1.23.2` is MIT-licensed. The installed wheel bundles `ThirdPartyNotices.txt` (326,866 bytes), preserved at `licenses/onnxruntime/ThirdPartyNotices.txt`. It must be read together with `licenses/onnxruntime/LICENSE`.

## Flatbuffers and Tokenizers license-text source

The installed `flatbuffers==25.12.19` and `tokenizers==0.22.1` wheels did not bundle license files. Their Apache-2.0 texts were copied from `google/flatbuffers` tag `v25.12.19` and `huggingface/tokenizers` tag `v0.22.1`, respectively, and are preserved under `licenses/flatbuffers/` and `licenses/tokenizers/`.
