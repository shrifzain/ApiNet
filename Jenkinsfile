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
        TARGET_SERVER = ''                       // Placeholder for target server, set later
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
                    env.TARGET_SERVER = inactiveEnv == 'blue' ? env.BLUE_SERVER : env.GREEN_SERVER
                    echo "Selected inactive environment: ${inactiveEnv} (${env.TARGET_SERVER})"
                }
            }
        }
        
        stage('Deploy to Server') {
            steps {
                withCredentials([sshUserPrivateKey(credentialsId: "${SSH_KEY_ID}", keyFileVariable: 'SSH_KEY')]) {
                    sh """
                        ssh -i \$SSH_KEY -v -o StrictHostKeyChecking=no ubuntu@${env.TARGET_SERVER} 'mkdir -p /home/ubuntu/app'
                        scp -i \$SSH_KEY -o StrictHostKeyChecking=no ${PUBLISH_DIR}/* ubuntu@${env.TARGET_SERVER}:/home/ubuntu/app/
                        ssh -i \$SSH_KEY -t -v -o StrictHostKeyChecking=no ubuntu@${env.TARGET_SERVER} '
                            sudo systemctl daemon-reload || { echo "daemon-reload failed"; exit 1; }
                            sudo systemctl enable pronet-api.service || { echo "enable failed"; exit 1; }
                            sudo systemctl restart pronet-api.service || { echo "restart failed"; exit 1; }
                            sudo systemctl status pronet-api.service --no-pager || { echo "status failed"; exit 1; }
                        '
                    """
                    
                    // Simple health check
                    sh "curl --fail http://${env.TARGET_SERVER}/ || exit 1"
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
