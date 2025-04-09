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
        
        stage('Deploy to Inactive Env') {
            steps {
                script {
                    def inactiveEnv = env.ACTIVE_ENV == 'blue' ? 'green' : 'blue'
                    def targetServer = inactiveEnv == 'blue' ? env.BLUE_SERVER : env.GREEN_SERVER
                    
                    echo "Deploying to ${inactiveEnv} (${targetServer})"
                    withCredentials([sshUserPrivateKey(credentialsId: "${SSH_KEY_ID}", keyFileVariable: 'SSH_KEY')]) {
                        sh """
                            ssh -i \$SSH_KEY -v -o StrictHostKeyChecking=no ubuntu@${targetServer} 'mkdir -p /home/ubuntu/app'
                            scp -i \$SSH_KEY -o StrictHostKeyChecking=no ${PUBLISH_DIR}/* ubuntu@${targetServer}:/home/ubuntu/app/
                            ssh -i \$SSH_KEY -v -o StrictHostKeyChecking=no ubuntu@${targetServer} '
                                sudo systemctl daemon-reload
                                sudo systemctl enable pronet-api.service
                                sudo systemctl restart pronet-api.service
                                sudo systemctl status pronet-api.service --no-pager
                            '
                        """
                    }
                    
                    // Simple health check
                    sh "curl --fail http://${targetServer}/ || exit 1"
                }
            }
        }
    }
    
    post {
        always {
            echo 'Cleaning up'
            cleanWs()
        }
    }
}
