#!/bin/bash
if [ "$GIT_AUTHOR_EMAIL" = "duydong@example.com" ]; then
    export GIT_AUTHOR_NAME="Truong Duy Dong"
    export GIT_AUTHOR_EMAIL="dong270603@gmail.com"
fi
if [ "$GIT_COMMITTER_EMAIL" = "duydong@example.com" ]; then
    export GIT_COMMITTER_NAME="Truong Duy Dong"
    export GIT_COMMITTER_EMAIL="dong270603@gmail.com"
fi
if [ "$GIT_AUTHOR_EMAIL" = "huyngo@example.com" ]; then
    export GIT_AUTHOR_NAME="HuyNgo"
    export GIT_AUTHOR_EMAIL="huyngo@gmail.com"
fi
if [ "$GIT_COMMITTER_EMAIL" = "huyngo@example.com" ]; then
    export GIT_COMMITTER_NAME="HuyNgo"
    export GIT_COMMITTER_EMAIL="huyngo@gmail.com"
fi
