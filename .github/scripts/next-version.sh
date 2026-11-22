#!/usr/bin/env bash
# Prints the next version to release.
#
# Directory.Build.props sets the major and minor version (VersionMajorMinor) and the version suffix
# (VersionSuffix, for example "preview"). The patch version is the highest patch of an existing tag
# <major>.<minor>.<patch>[-suffix] plus one. If no such tag exists, the patch version is 0.
#
# Usage: next-version.sh            reads the tags of the current repository
#        next-version.sh <tags...>  uses the given tags (for tests)
set -euo pipefail

project="src/ClosedXML.Parser/ClosedXML.Parser.csproj"
major_minor="$(dotnet msbuild "$project" -getProperty:VersionMajorMinor | tr -d '[:space:]')"
suffix="$(dotnet msbuild "$project" -getProperty:VersionSuffix | tr -d '[:space:]')"

if [[ -z "$major_minor" ]]; then
  echo "Directory.Build.props does not define VersionMajorMinor" >&2
  exit 1
fi

if [[ $# -gt 0 ]]; then tags=("$@"); else mapfile -t tags < <(git tag --list "${major_minor}.*"); fi

pattern="^${major_minor//./\\.}\\.([0-9]+)(-.+)?$"
patch=-1
for tag in "${tags[@]}"; do
  if [[ "$tag" =~ $pattern ]] && (( BASH_REMATCH[1] > patch )); then
    patch="${BASH_REMATCH[1]}"
  fi
done

version="${major_minor}.$((patch + 1))"
[[ -n "$suffix" ]] && version="${version}-${suffix}"
echo "$version"
