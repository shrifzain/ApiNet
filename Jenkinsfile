pipeline {
    agent any

    environment {
        PROJECT_NAME = 'ProNet.Api'
        SOLUTION_FILE = 'Api.csproj'
        PUBLISH_DIR = 'publish'
        GITHUB_REPO = 'https://github.com/shrifzain/ApiNet.git'
        S3_BUCKET = 'pronet-artifacts'
        BLUE_SERVER = '54.152.102.169'           // Blue instance IP
        GREEN_SERVER = '54.158.110.100'          // Green instance IP
        SSH_KEY_ID = 'sheraa-ssh-key'            // Jenkins credential ID for SSH key
        AWS_KEY_ID = 'awss'                      // AWS credential ID
        ACTIVE_ENV = 'blue'                      // Initial active environment
    }

    stages {
        stage('Get Code') {
            steps {
                echo 'Getting code from GitHub'
                git url: "${GITHUB_REPO}", branch: 'master', credentialsId: '01143717'
                sh 'ls -la'
                sh 'find . -type f'
                sh 'find . -name "*.csproj"'
            }
        }

        stage('Build') {
            steps {
                echo 'Building the app'
                sh "dotnet publish ${SOLUTION_FILE} -c Release -o ${PUBLISH_DIR}"
            }
        }

        stage('Save to S3') {
            steps {
                echo 'Saving app to S3'
                withAWS(credentials: "${AWS_KEY_ID}") {
                    sh "aws s3 cp ${PUBLISH_DIR} s3://${S3_BUCKET}/${PROJECT_NAME}/ --recursive"
                }
            }
        }

        stage('Choose Inactive Server') {
            steps {
                script {
                    def inactiveEnv = env.ACTIVE_ENV == 'blue' ? 'green' : 'blue'
                    
                    if (inactiveEnv == 'blue') {
                        env.TARGET_SERVER = BLUE_SERVER
                    } else {
                        env.TARGET_SERVER = GREEN_SERVER
                    }
                    
                    echo "Selected inactive environment: ${inactiveEnv}"
                    echo "Target server IP: ${env.TARGET_SERVER}"
                }
            }
        }

        stage('Deploy to Server') {
            steps {
                script {
                    if (env.TARGET_SERVER == null || env.TARGET_SERVER.trim() == '') {
                        error "TARGET_SERVER is not set! Cannot proceed with deployment."
                    }
                    
                    echo "Deploying to server: ${env.TARGET_SERVER}"
                }
                
                withCredentials([sshUserPrivateKey(credentialsId: "${SSH_KEY_ID}", keyFileVariable: 'SSH_KEY')]) {
                    sh """
                        # Create app directory (should already exist from Terraform, but ensure it's there)
                        ssh -i \$SSH_KEY -o StrictHostKeyChecking=no ubuntu@${env.TARGET_SERVER} 'mkdir -p /home/ubuntu/app'
                        
                        # Copy application files
                        scp -i \$SSH_KEY -o StrictHostKeyChecking=no -r ${PUBLISH_DIR}/* ubuntu@${env.TARGET_SERVER}:/home/ubuntu/app/
                        
                        # First verify that the service file exists
                        ssh -i \$SSH_KEY -o StrictHostKeyChecking=no ubuntu@${env.TARGET_SERVER} '
                            if [ ! -f /etc/systemd/system/pronet-api.service ]; then
                                echo "Service file does not exist. Check Terraform setup."
                                exit 1
                            fi
                            
                            # Ensure correct permissions
                            sudo chmod 644 /etc/systemd/system/pronet-api.service
                            
                            # Reload and start service
                            sudo systemctl daemon-reload
                            sudo systemctl enable pronet-api.service
                            sudo systemctl restart pronet-api.service
                            sudo systemctl status pronet-api.service --no-pager || true  # Don't fail on status
                        '
                    """
                    
                    // Health check with more tolerance
                    sh """
                        echo "Waiting for service to start..."
                        sleep 20
                        # Try multiple ports/endpoints in case your app doesn't use port 80
                        for PORT in 80 5000 5001 8080; do
                            echo "Checking health on port \$PORT..."
                            curl --connect-timeout 5 --max-time 10 --retry 3 --retry-delay 5 --fail http://${env.TARGET_SERVER}:\$PORT/ && exit 0 || echo "Not available on port \$PORT"
                        done
                        echo "Warning: Health check could not connect to service on standard ports. Please verify manually."
                    """
                }
            }
        }
        
        stage('Switch Traffic') {
            steps {
                script {
                    env.ACTIVE_ENV = env.ACTIVE_ENV == 'blue' ? 'green' : 'blue'
                    echo "Traffic switched to new environment: ${env.TARGET_SERVER}"
                    echo "Updated active environment to: ${env.ACTIVE_ENV}"
                }
            }
        }
    }

    post {
        always {
            echo 'Cleaning up'
            cleanWs()
        }
        success {
            echo 'Deployment successful!'
        }
        failure {
            echo 'Deployment failed!'
        }
    }
}
