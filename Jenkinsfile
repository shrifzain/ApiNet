pipeline {
    agent any
    
    environment {
        PROJECT_NAME = 'ProNet.Api'              // Name of the project
        SOLUTION_FILE = 'Api.csproj'             // .NET project file to build
        PUBLISH_DIR = 'publish'                  // Directory for build output
        GITHUB_REPO = 'https://github.com/shrifzain/ApiNet.git'  // GitHub repo URL
        S3_BUCKET = 'pronet-artifacts'           // S3 bucket for storing artifacts
        BLUE_SERVER = '54.152.102.169'           // Public IP of blue instance (from Terraform output)
        GREEN_SERVER = '54.158.110.100'          // Public IP of green instance (from Terraform output)
        SSH_KEY_ID = 'sheraa-ssh-key'            // SSH key ID for authentication
        AWS_KEY_ID = 'awss'                      // AWS credentials ID for S3 access
        ACTIVE_ENV = 'blue'                      // Initial active environment (blue or green)
    }
    
    stages {
        stage('Get Code') {
            steps {
                echo 'Getting code from GitHub'
                git url: "${GITHUB_REPO}", branch: 'master', credentialsId: '01143717'  // Clone repo
                sh 'ls -la'                          // List files in workspace (debugging)
                sh 'find . -type f'                  // List all files recursively (debugging)
                sh 'find . -name "*.csproj"'         // Locate .csproj file (debugging)
            }
        }
        
        stage('Build') {
            steps {
                echo 'Building the app'
                sh "dotnet publish ${SOLUTION_FILE} -c Release -o ${PUBLISH_DIR}"  // Build .NET app
            }
        }
        
        stage('Save to S3') {
            steps {
                echo 'Saving app to S3'
                withAWS(credentials: "${AWS_KEY_ID}") {  // Use AWS credentials
                    sh "aws s3 cp ${PUBLISH_DIR} s3://${S3_BUCKET}/${PROJECT_NAME}/ --recursive"  // Upload to S3
                }
            }
        }
        
        stage('Deploy to Inactive Env') {
            steps {
                script {
                    def inactiveEnv = env.ACTIVE_ENV == 'blue' ? 'green' : 'blue'  // Determine inactive env
                    def targetServer = inactiveEnv == 'blue' ? env.BLUE_SERVER : env.GREEN_SERVER  // Set target IP
                    
                    echo "Deploying to ${inactiveEnv} (${targetServer})"
                    sh """
                        scp -i \$SSH_KEY -o StrictHostKeyChecking=no ${PUBLISH_DIR}/* ubuntu@${targetServer}:/home/ubuntu/app/
                        ssh -i \$SSH_KEY -o StrictHostKeyChecking=no ubuntu@${targetServer} '
                            sudo systemctl daemon-reload          # Reload systemd config
                            sudo systemctl enable pronet-api.service  # Enable service on boot
                            sudo systemctl restart pronet-api.service  # Restart service
                            sudo systemctl status pronet-api.service --no-pager  # Check status
                        '
                    """
                    
                    // Simple health check
                    sh "curl --fail http://${targetServer}/ || exit 1"  // Verify API is running
                }
            }
        }
    }
    
    post {
        always {
            echo 'Cleaning up'
            cleanWs()  // Clean workspace after run
        }
    }
}
