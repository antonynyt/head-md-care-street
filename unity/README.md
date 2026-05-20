# Unity Code

This is the code, we will have our prototypes here.

## Collaborate

> This is the ideal way of doing it but we will not proceed that way beacause it's too complex for a small project.

### 1. Fork the Repository

Click the **Fork** button on GitHub to create your own copy under your account.

### 2. Clone Your Fork

```bash
git clone https://github.com/YOUR-USERNAME/head-md-care-street.git
cd head-md-care-street
```

Replace `YOUR-USERNAME` with your GitHub username.

### 3. Make Changes & Push

IMPORTANT: Create a branch (example: cup, bench, etc.), make your changes, and push to your fork (your github repo):

```bash
git checkout -b feature/your-feature-name
# ... make your changes ...
git add .
git commit -m "Describe your changes"
git push origin feature/your-feature-name
```

!!! KEEP your changes on your git branch (git checkout) !!!

### 4. Create a Pull Request

Go to the original repository and click **Compare & pull request**. Add a description of your changes and submit!

# Advanced Syncing

## Keep Your Fork Updated

If you want to sync your fork with the latest changes from the original repository:

```bash
# Add upstream remote (one time only)
git remote add upstream https://github.com/ORIGINAL-OWNER/head-md-care-street.git

# Fetch latest changes from original repo
git fetch upstream

# Merge upstream changes into your main branch
git checkout main
git merge upstream/main

# Push updates to your fork
git push origin main
```

## Rebase Before Pull Request

If the original repo has changed since you started your branch, rebase to keep history clean:

```bash
git fetch upstream
git rebase upstream/main
git push origin feature/your-feature-name --force-with-lease
```

This keeps your commits on top of the latest changes without merge commits.