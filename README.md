# VetRIS — Complete Deployment Lab

A hands-on, start-to-finish guide for deploying VetRIS (Radiology
Information System + PACS integration) from a completely fresh machine to
a running application, on AWS, with nothing skipped and nothing assumed.

Follow the sections in order — each one depends on the one before it. Every
command below is explained the same way:

> **Tool:** what you're using
> **Location:** exactly which directory to run it from
> **Purpose:** why this command exists
> **Expected result:** what you should see
> **Next step:** what comes after it

## Table of Contents

1. [Prerequisites](#1-prerequisites)
2. [Install Required Tools](#2-install-required-tools)
3. [Clone Repository](#3-clone-repository)
4. [Configure AWS CLI](#4-configure-aws-cli)
5. [Configure Project](#5-configure-project)
6. [Configure Database](#6-configure-database)
7. [Build Application](#7-build-application)
8. [Build Docker Image](#8-build-docker-image)
9. [Test Docker Container](#9-test-docker-container)
10. [Push Image to Container Registry](#10-push-image-to-container-registry)
11. [Deploy AWS Infrastructure with Terraform](#11-deploy-aws-infrastructure-with-terraform)
12. [Deploy Application](#12-deploy-application)
13. [Retrieve Application URL](#13-retrieve-application-url)
14. [Retrieve Database Endpoint](#14-retrieve-database-endpoint)
15. [Test Application](#15-test-application)
16. [Test Database Connection](#16-test-database-connection)
17. [Login to Application](#17-login-to-application)
18. [Verify Everything](#18-verify-everything)
19. [Troubleshooting](#19-troubleshooting)
20. [Terraform Destroy / Cleanup](#20-terraform-destroy--cleanup)

Everything below assumes **macOS, Linux, or Windows with WSL2** (all
commands are bash). It also assumes you own the domain used throughout as
an example, **alua2kg.online**, with:

- **Frontend** at `https://app.alua2kg.online`
- **Backend API** at `https://api.alua2kg.online`

Replace both with your own domain everywhere they appear if it's
different — search-and-replace `alua2kg.online` is the only thing you'd
need to change.

---

## 1. Prerequisites

Before starting, you need:

- An **AWS account** with billing enabled and permissions to create VPCs,
  RDS, ECS, ALBs, ECR, IAM roles, Route53 records, and ACM certificates
  (an `AdministratorAccess` policy is the simplest path for a lab; scope
  it down for anything beyond that).
- A **registered domain** (this guide uses `alua2kg.online`) with its
  hosted zone already in **Route53** — if it isn't, create the hosted
  zone first and point your domain's nameservers at it. This guide
  doesn't cover domain registration itself.
- A **GitHub account** with access to push to this repository, if you
  want the CI/CD pipeline (Section 11 onward can also be done by hand
  without GitHub Actions at all).
- **Root/administrator access** on your own machine, to install the
  tools in the next section.

---

## 2. Install Required Tools

Install these in order. Each has a verification command — run it after
installing, before moving to the next tool.

### 2.1 Git

> **Tool:** Git
> **Location:** anywhere
> **Purpose:** version control — you'll clone this repo and, eventually, commit changes to it.
> **Expected result:** version number printed
> **Next step:** install AWS CLI

```bash
# Ubuntu/Debian
sudo apt-get update && sudo apt-get install -y git

# macOS
brew install git
```

Verify:
```bash
git --version
```
Expected output: `git version 2.x.x` (any recent 2.x is fine).

### 2.2 AWS CLI v2

> **Tool:** AWS CLI
> **Location:** anywhere
> **Purpose:** every AWS interaction in this guide — authentication, retrieving outputs, running ECS/ECR commands — goes through this.
> **Expected result:** version number printed, showing `aws-cli/2.x.x`
> **Next step:** install Docker

```bash
# Linux
curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip"
unzip awscliv2.zip
sudo ./aws/install

# macOS
curl "https://awscli.amazonaws.com/AWSCLIV2.pkg" -o "AWSCLIV2.pkg"
sudo installer -pkg AWSCLIV2.pkg -target /
```

Verify:
```bash
aws --version
```
Expected output: `aws-cli/2.x.x Python/3.x.x ...`. If it shows `aws-cli/1.x`,
uninstall v1 first — v2 is required.

### 2.3 Docker

> **Tool:** Docker
> **Location:** anywhere
> **Purpose:** builds the backend container image and runs it locally for testing before it ever touches AWS.
> **Expected result:** version number printed, and the daemon responds
> **Next step:** install Terraform

Install **Docker Desktop** (macOS/Windows) from docker.com, or on Linux:
```bash
sudo apt-get update && sudo apt-get install -y docker.io
sudo usermod -aG docker $USER   # log out and back in after this
```

Verify:
```bash
docker --version
docker ps
```
Expected output: a version string, then an (probably empty) table of
running containers — if `docker ps` errors with a permission/connection
message, the daemon isn't running or your user isn't in the `docker` group
yet (see Troubleshooting §19.5).

### 2.4 Terraform

> **Tool:** Terraform
> **Location:** anywhere
> **Purpose:** provisions every piece of AWS infrastructure this app runs on — VPC, RDS, ECS, ALB, S3, IAM, Route53, ACM.
> **Expected result:** version number printed
> **Next step:** install the .NET 8 SDK

```bash
# Linux (HashiCorp's official apt repo)
wget -O- https://apt.releases.hashicorp.com/gpg | sudo gpg --dearmor -o /usr/share/keyrings/hashicorp-archive-keyring.gpg
echo "deb [signed-by=/usr/share/keyrings/hashicorp-archive-keyring.gpg] https://apt.releases.hashicorp.com $(lsb_release -cs) main" | sudo tee /etc/apt/sources.list.d/hashicorp.list
sudo apt-get update && sudo apt-get install -y terraform

# macOS
brew tap hashicorp/tap
brew install hashicorp/tap/terraform
```

Verify:
```bash
terraform version
```
Expected output: `Terraform v1.x.x`.

### 2.5 .NET 8 SDK

> **Tool:** .NET SDK
> **Location:** anywhere
> **Purpose:** builds, tests, and locally runs the backend before it's containerized.
> **Expected result:** version number printed starting with `8.`
> **Next step:** install a MySQL client

```bash
# Linux
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0
echo 'export PATH="$HOME/.dotnet:$PATH"' >> ~/.bashrc && source ~/.bashrc

# macOS
brew install --cask dotnet-sdk
```

Verify:
```bash
dotnet --version
```
Expected output: `8.0.x`.

### 2.6 MySQL client

> **Tool:** `mysql` CLI
> **Location:** anywhere
> **Purpose:** connecting directly to the database to verify connectivity and inspect data (Section 16).
> **Expected result:** version number printed
> **Next step:** clone the repository

```bash
# Ubuntu/Debian
sudo apt-get install -y mysql-client

# macOS
brew install mysql-client
echo 'export PATH="/opt/homebrew/opt/mysql-client/bin:$PATH"' >> ~/.zshrc && source ~/.zshrc
```

Verify:
```bash
mysql --version
```
Expected output: `mysql  Ver 8.x.x ...`.

---

## 3. Clone Repository

> **Tool:** Git
> **Location:** wherever you keep projects (e.g. `~/projects`)
> **Purpose:** get the actual source code onto your machine.
> **Expected result:** a new `VetRis` directory containing this repo
> **Next step:** configure the AWS CLI

```bash
git clone https://github.com/mdirfan738/VetRis.git
cd VetRis
```

Every command from here on states which directory to run it from —
**all paths given are relative to this `VetRis` directory**, referred to
below as the **repo root**.

---

## 4. Configure AWS CLI

> **Tool:** AWS CLI
> **Location:** anywhere (this is a one-time machine-wide setup)
> **Purpose:** gives every later `aws`/`terraform` command a way to authenticate to your AWS account.
> **Expected result:** your credentials are saved to `~/.aws/credentials`
> **Next step:** verify the identity, then move on to configuring the project

```bash
aws configure
```

You'll be prompted for four things, in order:
```
AWS Access Key ID [None]: <paste your access key>
AWS Secret Access Key [None]: <paste your secret key>
Default region name [None]: us-east-1
Default output format [None]: json
```

> Where do these keys come from? AWS Console → IAM → Users → your user →
> Security credentials → Create access key. If you're deploying this for
> real (not just a personal lab), prefer creating an IAM user scoped to
> exactly what Terraform needs rather than reusing a root account's keys.

Verify:
```bash
aws sts get-caller-identity
```

> **Tool:** AWS CLI
> **Location:** anywhere
> **Purpose:** confirms the CLI is actually authenticated as the account/user you expect, before you spend time on anything that depends on it.
> **Expected result:**
> ```json
> {
>     "UserId": "AIDAEXAMPLE...",
>     "Account": "123456789012",
>     "Arn": "arn:aws:iam::123456789012:user/your-username"
> }
> ```
> **Next step:** configure the project's environment variables

Write down the `Account` number — you'll need it in Section 10.

---

## 5. Configure Project

> **Tool:** a text editor
> **Location:** repo root
> **Purpose:** set up local environment variables for `docker-compose` (Section 9) and Terraform variables for the real AWS deployment (Section 11).
> **Expected result:** two new files, both gitignored, holding your actual values
> **Next step:** review the fixed database credentials (Section 6)

**5.1 — App-level env vars (for local Docker testing):**
```bash
cp .env.example .env
```
Open `.env` in your editor. The defaults work as-is for local testing —
you only need to fill in `AWS_ACCESS_KEY_ID`/`AWS_SECRET_ACCESS_KEY` if the
local container needs to reach S3 (DICOM upload testing); leave them blank
otherwise.

**5.2 — Terraform variables (for the real AWS deployment):**
```bash
cp terraform/envs/dev/terraform.tfvars.example terraform/envs/dev/terraform.tfvars
```
Open `terraform/envs/dev/terraform.tfvars` and replace every
`{{YOUR_...}}` placeholder:

| Placeholder | Replace with |
|---|---|
| `{{YOUR_DOMAIN}}` (appears in `domain_name`, `api_domain_name`, `backend_allowed_origin`) | your actual domain, e.g. `alua2kg.online` |
| `{{YOUR_ROUTE53_ZONE_ID}}` | your hosted zone ID — get it with `aws route53 list-hosted-zones-by-name --dns-name alua2kg.online --query "HostedZones[0].Id" --output text` |
| `{{YOUR_GITHUB_ORG}}` | your GitHub username/org, if using the CI/CD pipeline |
| `{{YOUR_TF_STATE_BUCKET}}` / `{{YOUR_TF_LOCK_TABLE}}` / `{{YOUR_ACCOUNT_ID}}` | from the Terraform state-bootstrap step, if this is the very first deployment (see `terraform/bootstrap/`) — if state is already bootstrapped, use the existing bucket/table names |

Both `.env` and `terraform.tfvars` are already in `.gitignore` — neither
ever gets committed.

---

## 6. Configure Database

> **Tool:** none — this is a review step, no commands to run yet
> **Location:** N/A
> **Purpose:** understand what's already been set up for you, and why.
> **Expected result:** N/A
> **Next step:** build the application

Per explicit requirement, this project uses **fixed database credentials**
rather than a randomly generated password:

```
DB Login ID: Admin
DB Password: AdminAdmin
```

(The password is `AdminAdmin`, not `Admin` — RDS MySQL requires a minimum
8-character master password, so this is the smallest possible change from
the literal requested value. See `terraform/modules/security/main.tf`'s
comment for the same note in code.)

This is already wired in for you in both places it's needed:
- **Local Docker** (`docker-compose.yml`) — the `mysql` service creates a
  user named `Admin` with password `AdminAdmin` on first startup.
- **AWS RDS** (`terraform/modules/security/main.tf` +
  `terraform/modules/database/main.tf`) — the same fixed value is stored
  in Secrets Manager and set as the RDS instance's master password.

Nothing to configure here — just know that `Admin`/`AdminAdmin` is the
credential you'll use in Sections 9 and 16.

---

## 7. Build Application

> **Tool:** .NET SDK
> **Location:** repo root
> **Purpose:** compile all four backend projects and confirm there are no build errors before wrapping any of it in Docker.
> **Expected result:** `Build succeeded.` with 0 errors
> **Next step:** run the test suite

```bash
dotnet restore VetRIS.sln
```
> Downloads every NuGet package the solution depends on. Expected result:
> completes with no red error text (warnings are fine).

```bash
dotnet build VetRIS.sln -c Release
```
> Compiles all 7 projects (4 application projects + 3 test projects).
> Expected result: ends with `Build succeeded.` and a count of 0 errors.

```bash
dotnet test VetRIS.sln
```
> **Tool:** .NET SDK (test runner)
> **Location:** repo root
> **Purpose:** runs all 68 automated tests — catches a real problem now, in seconds, instead of after a slow AWS deployment.
> **Expected result:** ends with `Passed!` and a summary line like
> `Passed: 68, Failed: 0, Skipped: 0`
> **Next step:** build the Docker image

If any test fails, stop here and fix it before continuing — see
`tests/README.md` for what each test project covers.

---

## 8. Build Docker Image

> **Tool:** Docker
> **Location:** repo root
> **Purpose:** package the compiled application into a container image — the same artifact that eventually runs in AWS.
> **Expected result:** `docker images` lists a new `vetris-backend:local` image
> **Next step:** test the container locally

```bash
cd backend
docker build -t vetris-backend:local .
```

This runs a **multi-stage build**: first stage compiles the solution
inside a full .NET SDK image, second stage copies just the compiled output
into a much smaller runtime-only image. Expect this to take 1-3 minutes
the first time (subsequent builds are faster thanks to Docker's layer
cache).

Verify:
```bash
docker images vetris-backend
```
Expected output: one row showing `vetris-backend`, tag `local`, and a size
around 200-250MB.

Return to the repo root before continuing:
```bash
cd ..
```

---

## 9. Test Docker Container

> **Tool:** Docker Compose
> **Location:** repo root
> **Purpose:** run the backend + a local MySQL together, exactly as they'll relate to each other in production, before anything touches AWS.
> **Expected result:** three containers running, health check passing
> **Next step:** push the image to ECR

```bash
docker-compose up --build
```

Leave this running in its own terminal. Expected result: log lines from
`vetris-mysql`, `vetris-backend`, and `vetris-frontend` interleaved, ending
with the backend logging that it's listening on port 8080. First startup
takes longer (MySQL initializing, NuGet/npm restoring) — subsequent
`docker-compose up` runs are much faster.

In a **second terminal**, verify the backend responds:
```bash
curl http://localhost:8080/api/health
```
> **Tool:** curl
> **Location:** anywhere
> **Purpose:** confirms the API is actually up and can reach its database, not just that the container started.
> **Expected result:** `{"status":"healthy","timestampUtc":"..."}`
> **Next step:** stop this local stack, then push the image to ECR

Stop the stack once you've confirmed it works:
```bash
docker-compose down
```

---

## 10. Push Image to Container Registry

Pushing needs a registry to push *to* — and that registry is itself
created by Terraform. Rather than run the entire infrastructure deploy
first, create just the registry now with a **targeted apply**, matching
the order this guide follows.

**10.1 — Initialize Terraform:**
```bash
cd terraform/envs/dev
terraform init
```
> **Tool:** Terraform
> **Location:** `terraform/envs/dev`
> **Purpose:** downloads the AWS provider plugin and configures the remote state backend.
> **Expected result:** ends with `Terraform has been successfully initialized!`
> **Next step:** create just the ECR repository

**10.2 — Create only the ECR repository:**
```bash
terraform apply -target=module.backend.aws_ecr_repository.backend
```
> **Tool:** Terraform
> **Location:** `terraform/envs/dev`
> **Purpose:** creates the one resource this section needs — the container registry — without deploying the rest of the infrastructure yet (that's Section 11).
> **Expected result:** prompts `Enter a value:` — type `yes` — then ends
> with `Apply complete! Resources: 1 added`
> **Next step:** log in to that registry

**10.3 — Get the registry URL:**
```bash
terraform output -raw ecr_repository_url
```
> **Expected result:** something like
> `123456789012.dkr.ecr.us-east-1.amazonaws.com/vetris-dev-backend`
> **Next step:** authenticate Docker against it

Save this value — export it so the next commands can reuse it:
```bash
export ECR_REPO=$(terraform output -raw ecr_repository_url)
```

**10.4 — Authenticate Docker to ECR:**
```bash
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin $ECR_REPO
```
> **Tool:** AWS CLI + Docker
> **Location:** anywhere (with `$ECR_REPO` set from the previous step)
> **Purpose:** gets a temporary token so `docker push` is allowed to write to this specific registry.
> **Expected result:** `Login Succeeded`
> **Next step:** tag and push the image

**10.5 — Tag and push:**
```bash
cd ../../..   # back to repo root
docker tag vetris-backend:local $ECR_REPO:latest
docker push $ECR_REPO:latest
```
> **Tool:** Docker
> **Location:** repo root
> **Purpose:** uploads the image you built and tested in Sections 8-9 to AWS, where ECS can pull it.
> **Expected result:** a progress bar per layer, ending with a digest line (`latest: digest: sha256:...`)
> **Next step:** deploy the rest of the AWS infrastructure

---

## 11. Deploy AWS Infrastructure with Terraform

> **Tool:** Terraform
> **Location:** `terraform/envs/dev`
> **Purpose:** creates everything else — VPC, subnets, security groups, RDS, S3, the ECS cluster/service/ALB, IAM roles, Route53 records, ACM certificates.
> **Expected result:** a plan showing dozens of resources to add, then all of them created
> **Next step:** confirm the deployment is running

```bash
cd terraform/envs/dev
terraform plan -out=tfplan
```
> **Expected result:** a detailed list of every resource Terraform intends
> to create (there will be many — this is a full production-shaped
> environment, not a toy). Read through it once; nothing should say
> "destroy" on a first deployment.
> **Next step:** apply that exact plan

```bash
terraform apply tfplan
```
> **Expected result:** resources are created one by one (this genuinely
> takes time — RDS alone can take 10-15 minutes to become available,
> and ACM DNS validation can take a few minutes too). Ends with
> `Apply complete! Resources: N added`.
> **Next step:** verify the ECS service reaches a stable, healthy state

Because you already pushed a real image in Section 10, the ECS service
Terraform creates here should start healthy on the first try — there's no
"deploy a placeholder, then push a real image later" gap to work around.

---

## 12. Deploy Application

The application is already running as of the `terraform apply` above — this
section is about confirming that, and knowing how to re-deploy after a
future code change.

**12.1 — Confirm the service is stable:**
```bash
aws ecs wait services-stable --cluster vetris-dev-cluster --services vetris-dev-backend
```
> **Tool:** AWS CLI
> **Location:** anywhere
> **Purpose:** blocks until ECS reports the service has reached its desired running-task count with passing health checks — the authoritative "is it actually up" signal, more reliable than just checking that `terraform apply` finished.
> **Expected result:** the command returns with no output and exit code 0
> (if it's still stabilizing, it waits — this can take a few minutes)
> **Next step:** retrieve the application URL

**12.2 — Future re-deploys** (after changing code and rebuilding the image):
```bash
docker tag vetris-backend:local $ECR_REPO:latest
docker push $ECR_REPO:latest
aws ecs update-service --cluster vetris-dev-cluster --service vetris-dev-backend --force-new-deployment
aws ecs wait services-stable --cluster vetris-dev-cluster --services vetris-dev-backend
```
> This is exactly what `.github/workflows/backend-deploy.yml` automates on
> every push to `main` — see Section 19.13 if a GitHub Actions deploy
> fails instead of a manual one.

---

## 13. Retrieve Application URL

```bash
cd terraform/envs/dev   # if not already there
terraform output -raw backend_api_url
```
> **Tool:** Terraform
> **Location:** `terraform/envs/dev`
> **Purpose:** get the real, DNS-backed URL Terraform just created — not a raw AWS-generated hostname.
> **Expected result:** `https://api.alua2kg.online`
> **Next step:** retrieve the database endpoint

---

## 14. Retrieve Database Endpoint

```bash
terraform output -raw db_endpoint
```
> **Tool:** Terraform
> **Location:** `terraform/envs/dev`
> **Purpose:** get the RDS instance's actual connection hostname.
> **Expected result:** something like
> `vetris-dev-mysql.abc123xyz.us-east-1.rds.amazonaws.com`
> (this output is marked sensitive in Terraform, which is why `-raw` is
> needed — without it, `terraform output db_endpoint` would print
> `<sensitive>` instead of the value)
> **Next step:** test that the application is actually reachable

The database port is fixed at **3306** (MySQL's default, set explicitly in
`terraform/modules/database/main.tf`) and the database name is **`vetris`**.

---

## 15. Test Application

```bash
curl https://api.alua2kg.online/api/health
```
> **Tool:** curl
> **Location:** anywhere
> **Purpose:** confirms the real, publicly-deployed API responds — not just that AWS resources exist, but that traffic actually reaches a healthy container.
> **Expected result:** `{"status":"healthy","timestampUtc":"..."}`
> **Next step:** test the database connection directly

If this hangs or fails, DNS may not have propagated yet (ACM validation +
Route53 alias records can take a few minutes after `terraform apply`
finishes) — wait 2-3 minutes and retry before moving to Troubleshooting.

---

## 16. Test Database Connection

```bash
mysql -h $(cd terraform/envs/dev && terraform output -raw db_endpoint) -P 3306 -u Admin -pAdminAdmin vetris -e "SHOW TABLES;"
```
> **Tool:** MySQL CLI
> **Location:** repo root (the subshell handles fetching the endpoint)
> **Purpose:** confirms you can actually authenticate and query the real
> production database, not just that the API's health check passes.
> **Expected result:** a list of table names (`Studies`, `Reports`,
> `Invoices`, `AspNetUsers`, etc. — created by EF Core migrations)
> **Next step:** log in to the application itself

> **Networking note:** this only works if your current machine's IP is
> allowed to reach the RDS instance. If it hangs or times out, RDS is
> almost certainly in a private subnet (correct, secure default) — see
> Troubleshooting §19.11 for how to test connectivity from inside the VPC
> instead (e.g., via an ECS Exec session into the running backend task).

---

## 17. Login to Application

Per explicit requirement, the application login is also fixed:

```
Application Login ID: Admin
Application Password: Admin
```

```bash
curl -X POST https://api.alua2kg.online/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@alua2kg.online","password":"Admin"}'
```
> **Tool:** curl
> **Location:** anywhere
> **Purpose:** confirms the fixed admin account actually works end-to-end — password hashing, database lookup, JWT issuance.
> **Expected result:** a JSON response containing an `accessToken` field
> (a long string starting with `eyJ...`)
> **Next step:** verify everything together

> Login is by **email**, not username — the seeded account's email is
> `admin@<yourdomain>` (constructed automatically from the username
> `Admin`; see `SeedInitialAdminAsync` in
> `backend/VetRIS.Api/Program.cs`). If you changed the domain, adjust the
> email in this command to match.

From the frontend, the same credentials go in the normal login form at
`https://app.alua2kg.online`.

---

## 18. Verify Everything

Run back through this checklist — every item should be true by now:

- [ ] `dotnet test VetRIS.sln` passes all 68 tests (Section 7)
- [ ] `docker images vetris-backend` shows the local image (Section 8)
- [ ] `docker-compose up --build` ran successfully and `/api/health`
      responded locally (Section 9)
- [ ] `terraform output -raw ecr_repository_url` returns a real ECR URL
      (Section 10)
- [ ] `docker push` completed with a digest (Section 10)
- [ ] `terraform apply tfplan` completed with `Apply complete!` (Section 11)
- [ ] `aws ecs wait services-stable` returned with no error (Section 12)
- [ ] `terraform output -raw backend_api_url` returns
      `https://api.alua2kg.online` (Section 13)
- [ ] `terraform output -raw db_endpoint` returns a real RDS hostname
      (Section 14)
- [ ] `curl https://api.alua2kg.online/api/health` returns `"healthy"`
      (Section 15)
- [ ] `mysql -h <endpoint> -u Admin -pAdminAdmin vetris` connects and
      lists tables (Section 16)
- [ ] Logging in with `Admin`/`Admin` returns a JWT (Section 17)

If every box is checked, the deployment is complete and working
end-to-end.

---

## 19. Troubleshooting

Format for each: **Problem → Possible Cause → Command to Check → How to
Fix → Expected Result**.

### 19.1 Terraform initialization failure

- **Problem:** `terraform init` fails.
- **Possible cause:** no network access to the Terraform registry, or the
  remote state backend (S3 bucket/DynamoDB table) doesn't exist yet.
- **Command to check:** `aws s3 ls s3://<your-state-bucket>`
- **Fix:** if the bucket doesn't exist, run the bootstrap step first
  (`terraform/bootstrap/`, which creates the state bucket and lock table
  before `envs/dev` can use them).
- **Expected result:** `terraform init` ends with
  `Terraform has been successfully initialized!`

### 19.2 Terraform plan/apply failure

- **Problem:** `terraform plan` or `apply` errors out mid-way.
- **Possible cause:** almost always a missing/incorrect value in
  `terraform.tfvars`, or an AWS service quota limit.
- **Command to check:** re-read the error message — Terraform's AWS
  provider errors usually name the exact resource and reason.
- **Fix:** correct the `.tfvars` value, or request a quota increase (AWS
  Console → Service Quotas) if the error mentions a limit.
- **Expected result:** `terraform plan` runs clean; `terraform apply`
  completes with `Apply complete!`

### 19.3 AWS authentication failure

- **Problem:** any `aws` or `terraform` command fails with
  `Unable to locate credentials` or `ExpiredToken`.
- **Possible cause:** `aws configure` was never run, or temporary
  credentials (if using SSO/assumed roles) expired.
- **Command to check:** `aws sts get-caller-identity`
- **Fix:** re-run `aws configure` (Section 4), or `aws sso login` if using
  SSO.
- **Expected result:** `get-caller-identity` returns your account/user
  info without error.

### 19.4 IAM permission errors

- **Problem:** a command fails with `AccessDenied` or
  `is not authorized to perform`.
- **Possible cause:** the IAM user/role doesn't have the permission the
  failing action needs.
- **Command to check:** the error message names the exact action (e.g.
  `ecs:CreateCluster`) — check it against your IAM user's attached
  policies in the AWS Console.
- **Fix:** attach the missing permission, or use a broader policy like
  `AdministratorAccess` for lab purposes.
- **Expected result:** the same command succeeds after the permission is
  added.

### 19.5 Docker build failure

- **Problem:** `docker build` fails.
- **Possible cause:** Docker daemon not running, or a `dotnet restore`
  failure inside the build (network issue reaching NuGet).
- **Command to check:** `docker ps` (confirms the daemon is up);
  re-run with `docker build --progress=plain -t vetris-backend:local .`
  for full output.
- **Fix:** start Docker Desktop / `sudo systemctl start docker`; retry the
  build if it was a transient network issue.
- **Expected result:** build completes, `docker images` shows the new
  image.

### 19.6 Docker container not starting

- **Problem:** `docker-compose up` starts but the backend container exits
  immediately or restarts in a loop.
- **Possible cause:** missing/invalid environment variables (especially
  `Jwt__Key` — the app throws on startup if it's empty) or MySQL not
  ready yet.
- **Command to check:** `docker-compose logs backend`
- **Fix:** check the log for the specific startup exception; confirm `.env`
  has a `VETRIS_DEV_JWT_KEY` value; confirm `depends_on: mysql: condition:
  service_healthy` is working (`docker-compose ps` should show `mysql` as
  healthy before `backend` starts).
- **Expected result:** `docker-compose logs backend` shows a normal
  ASP.NET Core startup log ending with "Now listening on...".

### 19.7 Container image push failure

- **Problem:** `docker push` fails with `no basic auth credentials` or
  `denied`.
- **Possible cause:** the ECR login token expired (they last 12 hours), or
  you're pushing to the wrong registry URL.
- **Command to check:** `echo $ECR_REPO` (confirm it's the real ECR URL,
  not a typo)
- **Fix:** re-run the `aws ecr get-login-password | docker login` command
  from Section 10.4.
- **Expected result:** `docker push` shows progress and completes with a
  digest.

### 19.8 ECS/service deployment failure

- **Problem:** the ECS service never reaches steady state; `aws ecs wait
  services-stable` times out.
- **Possible cause:** the task can't pull the image (wrong tag/permissions),
  can't reach the database (security group/subnet issue), or is crashing
  on startup (bad config).
- **Command to check:**
  ```bash
  aws ecs describe-services --cluster vetris-dev-cluster --services vetris-dev-backend --query 'services[0].events[0:5]'
  ```
- **Fix:** the events list usually names the exact problem (e.g.
  "unable to pull image", "task failed ELB health checks"). Cross-reference
  with CloudWatch Logs (log group
  `/vetris/dev/backend` or `/vetris/dev/backend-ecs` — see
  `terraform/modules/security/main.tf`) for the actual application error.
- **Expected result:** after the fix, `aws ecs wait services-stable`
  returns cleanly.

### 19.9 Load balancer health-check failure

- **Problem:** ECS tasks start but the ALB target group shows them as
  unhealthy.
- **Possible cause:** the security-group rule from the ALB to the backend
  tasks is missing (shouldn't happen — `terraform/modules/backend/main.tf`
  creates this explicitly), or the container isn't actually listening on
  the port the target group checks (8080).
- **Command to check:**
  ```bash
  aws elbv2 describe-target-health --target-group-arn $(cd terraform/envs/dev && terraform output -raw target_group_arn 2>/dev/null || echo "<get from module.backend.target_group_arn>")
  ```
- **Fix:** confirm `ASPNETCORE_URLS=http://+:8080` is set (it is, by
  default, in the Dockerfile) and that `/api/health` returns 200 when
  called from inside the VPC (via ECS Exec — see §19.11).
- **Expected result:** target health shows `healthy`.

### 19.10 Application not reachable

- **Problem:** `curl https://api.alua2kg.online/api/health` hangs or
  fails to resolve.
- **Possible cause:** DNS hasn't propagated yet, or the Route53 record/ACM
  certificate wasn't created successfully.
- **Command to check:** `dig api.alua2kg.online` (should return the ALB's
  hostname via an ALIAS record)
- **Fix:** wait a few minutes for DNS propagation; if `dig` returns
  nothing after 10+ minutes, check `terraform apply`'s output for ACM
  validation or Route53 errors.
- **Expected result:** `dig` resolves; `curl` returns the health response.

### 19.11 Database connection failure

- **Problem:** `mysql -h <endpoint> ...` from Section 16 hangs or times
  out.
- **Possible cause:** RDS is correctly placed in a private subnet, so your
  local machine can't reach it directly — this is expected, not a bug.
- **Command to check:** connect from *inside* the VPC instead, via an ECS
  Exec session into the running backend task:
  ```bash
  aws ecs execute-command --cluster vetris-dev-cluster \
    --task $(aws ecs list-tasks --cluster vetris-dev-cluster --service-name vetris-dev-backend --query 'taskArns[0]' --output text) \
    --container backend --interactive --command "/bin/bash"
  ```
  (then run `mysql` from inside that shell)
- **Fix:** this is the correct way to test DB connectivity for a
  privately-networked RDS instance — no fix needed, just a different
  testing method than a direct connection from your laptop.
- **Expected result:** connects successfully from inside the container.

### 19.12 Security group / networking problems

- **Problem:** any AWS-to-AWS connection fails (ECS→RDS, ALB→ECS).
- **Possible cause:** a missing security-group ingress rule.
- **Command to check:**
  ```bash
  aws ec2 describe-security-groups --group-ids <sg-id> --query 'SecurityGroups[0].IpPermissions'
  ```
- **Fix:** this project's security groups are defined in
  `terraform/modules/network/main.tf` (base rules) and
  `terraform/modules/backend/main.tf` (the ALB→ECS rule added on top) —
  compare against what's actually applied; re-run `terraform apply` if
  they've drifted from what's in code.
- **Expected result:** the required rule is present.

### 19.13 GitHub Actions authentication failure

- **Problem:** `.github/workflows/backend-deploy.yml` fails at the
  "Configure AWS credentials (OIDC)" step.
- **Possible cause:** the `AWS_ROLE_ARN` secret isn't set in the GitHub
  repo, or the OIDC trust relationship on that IAM role doesn't allow this
  specific repo/branch.
- **Command to check:** GitHub repo → Settings → Secrets and variables →
  Actions → confirm `AWS_ROLE_ARN` exists under the `dev-backend`
  environment.
- **Fix:** set the secret if missing; check
  `terraform/modules/iam-oidc/main.tf`'s trust policy condition matches
  your actual `github_org`/`github_repo` variables.
- **Expected result:** the workflow's AWS credentials step succeeds.

### 19.14 GitHub Actions deployment failure

- **Problem:** the workflow authenticates fine but fails later (build,
  push, migrate, or deploy step).
- **Possible cause:** varies — check which step failed.
- **Command to check:** GitHub repo → Actions tab → click the failed run →
  read the specific step's log.
- **Fix:** the migration step needs network access to RDS (see §19.11's
  networking note — this applies to the CI runner too); the deploy step
  needs the `ECS_CLUSTER_NAME`/`ECS_SERVICE_NAME`/`ECS_TASK_FAMILY`
  secrets set correctly.
- **Expected result:** all steps show green checkmarks.

### 19.15 Application login failure

- **Problem:** `POST /api/auth/login` with `Admin`/`Admin` returns 401.
- **Possible cause:** the app hasn't started up yet since the last
  deployment (the admin account is created once, on startup, by
  `SeedInitialAdminAsync`), or `VETRIS_ADMIN_USERNAME`/
  `VETRIS_ADMIN_PASSWORD` weren't set on the running task.
- **Command to check:**
  ```bash
  aws logs tail /vetris/dev/backend --since 10m | grep -i admin
  ```
- **Fix:** confirm those two env vars are present in the ECS task
  definition (`aws ecs describe-task-definition --task-definition
  vetris-dev-backend --query 'taskDefinition.containerDefinitions[0].secrets'`)
  — they're wired from Secrets Manager in
  `terraform/modules/backend/main.tf`; force a new deployment
  (`aws ecs update-service ... --force-new-deployment`) to retry seeding.
- **Expected result:** login succeeds and returns a JWT.

---

## 20. Terraform Destroy / Cleanup

> **Tool:** Terraform
> **Location:** `terraform/envs/dev`
> **Purpose:** tears down every AWS resource this guide created, so you
> stop being billed for them.
> **Expected result:** all resources removed
> **Next step:** none — you're done

```bash
cd terraform/envs/dev
terraform destroy
```

Type `yes` when prompted. This removes the VPC, RDS instance, ECS
cluster/service, ALB, S3 buckets, IAM roles, Route53 records, and ACM
certificates — everything Sections 10-11 created.

> The ECR repository is configured with `force_delete = true`
> specifically so this step can remove it even with images still pushed
> to it — no separate "empty the registry first" step needed.

Verify nothing billable is left running:
```bash
aws ecs list-clusters
aws rds describe-db-instances --query 'DBInstances[*].DBInstanceIdentifier'
```
Both should return empty lists once `terraform destroy` finishes.

If you also ran the ECR-only apply in Section 10.2 as a genuinely separate
step and `destroy` doesn't clean it up for some reason, remove it directly:
```bash
aws ecr delete-repository --repository-name vetris-dev-backend --force
```
