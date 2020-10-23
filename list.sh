#!/bin/bash
#
# Lists the filesystem items (files, folders) that are in the current working folder.
#
# Usage:
#
#     ./list.sh
#

for i in *; do
  echo $i
done