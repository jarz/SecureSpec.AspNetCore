#!/usr/bin/env python3
"""
Script to create GitHub issues from the issues.json file.
Requires: pip install PyGithub
Usage: python create-issues.py [--dry-run] [--token YOUR_GITHUB_TOKEN]
"""

import argparse
import json
import os
import sys
import time
from typing import Dict, List

try:
    from github import Github
    from github.GithubException import GithubException
except ImportError:
    print("Error: PyGithub not installed")
    print("Install it with: pip install PyGithub")
    sys.exit(1)


def load_issues(filename: str = "issues.json") -> List[Dict]:
    """Load issues from JSON file."""
    with open(filename, 'r') as f:
        data = json.load(f)
    return data['issues']


def format_issue_body(issue: Dict) -> str:
    """Format the issue body from the issue dictionary."""
    body = f"""## Description
{issue['description']}

## Estimated Effort
{issue['effort_days']} days

## Priority
{issue['priority'].upper()}

## Dependencies
"""
    
    if issue['dependencies']:
        for dep in issue['dependencies']:
            body += f"- Issue {dep}\n"
    else:
        body += "None\n"
    
    body += "\n## Acceptance Criteria\n"
    for ac in issue['acceptance_criteria']:
        body += f"- {ac}\n"
    
    # Add phase info
    body += f"\n## Phase\n{issue['phase']}\n"
    
    return body


def create_github_issues(token: str, repo_name: str, issues: List[Dict], dry_run: bool = False):
    """Create GitHub issues using the GitHub API."""
    
    # Initialize GitHub client
    g = Github(token)
    
    try:
        repo = g.get_repo(repo_name)
        print(f"Connected to repository: {repo_name}")
    except GithubException as e:
        print(f"Error accessing repository: {e}")
        sys.exit(1)
    
    # Track created issues for dependency linking
    created_issues = {}
    
    # Sort issues by phase and ID to ensure dependencies are created first
    issues_sorted = sorted(issues, key=lambda x: (
        0 if x['phase'] == 'cross-cutting' else x['phase'],
        x['id']
    ))
    
    print(f"\nWill create {len(issues_sorted)} issues")
    if dry_run:
        print("DRY RUN MODE - No issues will be created\n")
    else:
        print()
    
    for issue_data in issues_sorted:
        issue_id = issue_data['id']
        phase = issue_data['phase']
        title = f"Phase {phase}: {issue_data['title']}" if phase != 'cross-cutting' else issue_data['title']
        
        # Format body
        body = format_issue_body(issue_data)
        
        # Add dependency links if issues were already created
        if issue_data['dependencies']:
            body += "\n## Related Issues\n"
            for dep_id in issue_data['dependencies']:
                if dep_id in created_issues:
                    issue_number = created_issues[dep_id]
                    body += f"- Depends on #{issue_number}\n"
        
        # Format labels
        labels = issue_data['labels']
        
        if dry_run:
            print(f"Would create: [{issue_id}] {title}")
            print(f"  Labels: {', '.join(labels)}")
            print(f"  Priority: {issue_data['priority']}")
            print()
        else:
            try:
                print(f"Creating: [{issue_id}] {title}")
                issue = repo.create_issue(
                    title=title,
                    body=body,
                    labels=labels
                )
                created_issues[issue_id] = issue.number
                print(f"  ✓ Created issue #{issue.number}")
                
                # Rate limiting - GitHub allows 5000 requests/hour for authenticated users
                # but we'll be conservative
                time.sleep(1)
                
            except GithubException as e:
                print(f"  ✗ Error creating issue: {e}")
                if e.status == 403:
                    print("  Rate limit may be exceeded. Waiting 60 seconds...")
                    time.sleep(60)
                    continue
    
    print(f"\n{'Dry run complete' if dry_run else 'Issue creation complete'}!")
    if not dry_run:
        print(f"Created {len(created_issues)} issues")
        print(f"View at: https://github.com/{repo_name}/issues")


def main():
    parser = argparse.ArgumentParser(
        description='Create GitHub issues from issues.json'
    )
    parser.add_argument(
        '--dry-run',
        action='store_true',
        help='Print what would be created without actually creating issues'
    )
    parser.add_argument(
        '--token',
        type=str,
        default=os.environ.get('GITHUB_TOKEN'),
        help='GitHub personal access token (or set GITHUB_TOKEN env var)'
    )
    parser.add_argument(
        '--repo',
        type=str,
        default='jarz/SecureSpec.AspNetCore',
        help='Repository in format owner/repo'
    )
    parser.add_argument(
        '--file',
        type=str,
        default='issues.json',
        help='Path to issues.json file'
    )
    
    args = parser.parse_args()
    
    # Validate token
    if not args.dry_run and not args.token:
        print("Error: GitHub token required")
        print("Provide via --token argument or GITHUB_TOKEN environment variable")
        print("Create a token at: https://github.com/settings/tokens")
        print("Required scopes: repo")
        sys.exit(1)
    
    # Load issues
    try:
        script_dir = os.path.dirname(os.path.abspath(__file__))
        issues_file = os.path.join(script_dir, args.file)
        issues = load_issues(issues_file)
        print(f"Loaded {len(issues)} issues from {args.file}")
    except FileNotFoundError:
        print(f"Error: {args.file} not found")
        sys.exit(1)
    except json.JSONDecodeError as e:
        print(f"Error parsing JSON: {e}")
        sys.exit(1)
    
    # Create issues
    create_github_issues(args.token, args.repo, issues, args.dry_run)


if __name__ == '__main__':
    main()
