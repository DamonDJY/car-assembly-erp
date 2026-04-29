#!/bin/bash
# 此脚本用于在 GitHub 创建仓库并推送代码
# 前提：已安装 gh CLI 并已登录 (gh auth login)

# 创建新的 GitHub 仓库（公开/私有任选）
gh repo create car-assembly-erp --public --source=. --remote=origin --push

# 如果希望私有仓库，将 --public 改为 --private
# gh repo create car-assembly-erp --private --source=. --remote=origin --push
